import test from "node:test";
import assert from "node:assert/strict";
import { buildMembershipApplicationPersistentSlots } from "./membership-application-flow-service.ts";
import { canStartMembershipApplication, membershipBoardDecisionPath } from "./membership-application-lifecycle.ts";
import { usesPersistentMembershipSignatures } from "../../../supabase/functions/kgv-generate-contract/membership-application-policy.ts";

type AdmissionStatus = "antrag_offen" | "aufgenommen" | "abgelehnt";
type Application = {
  id: number;
  number: number | null;
  status: "vorbereitet" | "zur_signatur" | "letzte_pruefung" | "erledigt";
  signatures: Set<string>;
  finalDocumentId: number | null;
  boardReviewId: number | null;
  votes: Map<number, { value: "ja" | "nein"; internalReason: string | null; id: number }>;
  decision: "offen" | "angenommen" | "abgelehnt";
  outwardRejectionReason: string | null;
  confirmationProcessId: number | null;
  confirmationDocumentId: number | null;
  dispatchId: number | null;
};

class MembershipApplicationHarness {
  member = { id: 1081, admissionStatus: "antrag_offen" as AdmissionStatus, active: false, memberSince: null as string | null };
  applications: Application[] = [];
  representativeLinks = new Map<number, number>();
  representatives = new Map<number, { id: number; createdAt: string; existing: boolean }>();
  nextApplicationId = 41;
  nextApplicationNumber = 10017;
  nextObjectId = 501;

  start() {
    const active = this.applications.find(item => item.decision === "offen" && item.status !== "erledigt");
    if (active) return active;
    if (this.member.admissionStatus === "aufgenommen") throw new Error("already admitted");
    if (this.member.admissionStatus === "abgelehnt") this.member.admissionStatus = "antrag_offen";
    const application: Application = { id: this.nextApplicationId++, number: null, status: "vorbereitet", signatures: new Set(), finalDocumentId: null, boardReviewId: null, votes: new Map(), decision: "offen", outwardRejectionReason: null, confirmationProcessId: null, confirmationDocumentId: null, dispatchId: null };
    this.applications.push(application);
    return application;
  }

  startSigning(application: Application) {
    if (application.status === "vorbereitet") application.status = "zur_signatur";
    if (application.number === null) application.number = this.nextApplicationNumber++;
    return application;
  }

  sign(application: Application, requiredKeys: string[]) {
    for (const key of requiredKeys) application.signatures.add(key);
  }

  finalize(application: Application, requiredKeys: string[]) {
    if (!requiredKeys.every(key => application.signatures.has(key))) throw new Error("missing signature");
    application.finalDocumentId ??= this.nextObjectId++;
    application.status = "letzte_pruefung";
    return application.finalDocumentId;
  }

  startBoard(application: Application, authorized = true) {
    if (!authorized) throw new Error("forbidden");
    application.boardReviewId ??= this.nextObjectId++;
    return application.boardReviewId;
  }

  vote(application: Application, personId: number, value: "ja" | "nein", internalReason: string | null = null) {
    const previous = application.votes.get(personId);
    if (previous?.value === value && previous.internalReason === internalReason) return previous.id;
    const vote = { value, internalReason, id: this.nextObjectId++ };
    application.votes.set(personId, vote);
    return vote.id;
  }

  decide(application: Application, result: "annehmen" | "ablehnen", path: "einstimmig" | "vorstandssitzung", authorized = true, manualRepresentative = false) {
    if (!authorized) throw new Error("forbidden");
    const expected = result === "annehmen" ? "angenommen" : "abgelehnt";
    if (application.decision !== "offen") {
      if (application.decision === expected) return application.decision;
      throw new Error("already decided differently");
    }
    if (path === "einstimmig" && [...application.votes.values()].some(vote => vote.value !== "ja")) throw new Error("not unanimous");
    if (manualRepresentative && result === "annehmen") {
      const existing = this.representativeLinks.get(this.member.id);
      if (!existing) {
        const id = this.nextObjectId++;
        this.representatives.set(id, { id, createdAt: "2026-10-28T10:00:00Z", existing: false });
        this.representativeLinks.set(this.member.id, id);
      }
    }
    application.decision = result === "annehmen" ? "angenommen" : "abgelehnt";
    application.status = "erledigt";
    this.member.admissionStatus = result === "annehmen" ? "aufgenommen" : "abgelehnt";
    this.member.active = result === "annehmen";
    this.member.memberSince = result === "annehmen" ? "2026-11-01" : null;
    if (result === "annehmen") application.confirmationProcessId ??= this.nextObjectId++;
    return application.decision;
  }

  confirm(application: Application) {
    if (application.decision !== "angenommen") throw new Error("not admitted");
    application.confirmationProcessId ??= this.nextObjectId++;
    application.confirmationDocumentId ??= this.nextObjectId++;
    return application.confirmationDocumentId;
  }

  dispatch(application: Application) {
    application.dispatchId ??= this.nextObjectId++;
    return application.dispatchId;
  }

  snapshot() {
    return JSON.stringify({ member: this.member, applications: this.applications.map(item => ({ ...item, signatures: [...item.signatures], votes: [...item.votes] })) });
  }
}

const adultKeys = buildMembershipApplicationPersistentSlots({ isMinor: false, includeSecondaryMember: false, memberId: 1081 }).map(item => item.key);
const minorExistingKeys = buildMembershipApplicationPersistentSlots({ isMinor: true, includeSecondaryMember: false, memberId: 1081, representativeMemberId: 2077 }).map(item => item.key);
const minorManualKeys = buildMembershipApplicationPersistentSlots({ isMinor: true, includeSecondaryMember: false, memberId: 1081 }).map(item => item.key);

test("Happy Path A: adult application remains idempotent through confirmation and dispatch", () => {
  const flow = new MembershipApplicationHarness();
  const application = flow.start();
  assert.equal(flow.start(), application);
  assert.equal(flow.startSigning(application), application);
  const reserved = application.number;
  flow.startSigning(application);
  assert.equal(application.number, reserved);
  assert.equal(reserved, 10017);
  assert.throws(() => flow.finalize(application, adultKeys), /missing signature/);
  flow.sign(application, adultKeys);
  const document = flow.finalize(application, adultKeys);
  assert.equal(flow.finalize(application, adultKeys), document);
  assert.equal(flow.startBoard(application), flow.startBoard(application));
  const vote1 = flow.vote(application, 1, "ja");
  assert.equal(flow.vote(application, 1, "ja"), vote1);
  flow.vote(application, 2, "ja");
  assert.equal(application.decision, "offen", "unanimous votes must not decide automatically");
  assert.equal(membershipBoardDecisionPath(true), "einstimmig");
  flow.decide(application, "annehmen", "einstimmig");
  assert.equal(flow.decide(application, "annehmen", "einstimmig"), "angenommen");
  const confirmation = flow.confirm(application);
  assert.equal(flow.confirm(application), confirmation);
  const dispatch = flow.dispatch(application);
  assert.equal(flow.dispatch(application), dispatch);
  assert.deepEqual(flow.member, { id: 1081, admissionStatus: "aufgenommen", active: true, memberSince: "2026-11-01" });
});

test("Happy Path B: minor reuses an existing representative and requires both representative signatures", () => {
  const flow = new MembershipApplicationHarness();
  flow.representatives.set(2077, { id: 2077, createdAt: "2020-01-01T00:00:00Z", existing: true });
  flow.representativeLinks.set(flow.member.id, 2077);
  const application = flow.startSigning(flow.start());
  flow.sign(application, minorExistingKeys);
  flow.finalize(application, minorExistingKeys);
  assert.equal(flow.representativeLinks.get(flow.member.id), 2077);
  assert.equal(flow.representatives.size, 1);
  assert.ok(minorExistingKeys.includes("gesetzliche_vertretung.antrag"));
  assert.ok(minorExistingKeys.includes("gesetzliche_vertretung.datenschutz"));
});

test("Happy Path C: a manual representative is created once on acceptance", () => {
  const flow = new MembershipApplicationHarness();
  const application = flow.startSigning(flow.start());
  flow.sign(application, minorManualKeys);
  flow.finalize(application, minorManualKeys);
  flow.startBoard(application);
  flow.vote(application, 1, "ja"); flow.vote(application, 2, "ja");
  flow.decide(application, "annehmen", "einstimmig", true, true);
  assert.equal(flow.representatives.size, 1);
  assert.ok([...flow.representatives.values()][0].createdAt);
  assert.equal(flow.decide(application, "annehmen", "einstimmig", true, true), "angenommen");
  assert.equal(flow.representatives.size, 1);
});

test("rejection remains a chair decision, keeps internal reasons internal, and permits a fresh numbered application", () => {
  const flow = new MembershipApplicationHarness();
  const first = flow.startSigning(flow.start());
  flow.sign(first, adultKeys); flow.finalize(first, adultKeys); flow.startBoard(first);
  flow.vote(first, 1, "ja"); flow.vote(first, 2, "nein", "interner Zweifel");
  assert.equal(first.decision, "offen");
  assert.equal(membershipBoardDecisionPath(false), "vorstandssitzung");
  flow.decide(first, "ablehnen", "vorstandssitzung");
  assert.equal(first.outwardRejectionReason, null);
  assert.equal(first.votes.get(2)?.internalReason, "interner Zweifel");
  assert.equal(canStartMembershipApplication(flow.member.admissionStatus, "signed"), true);
  const second = flow.startSigning(flow.start());
  assert.notEqual(second.id, first.id);
  assert.notEqual(second.number, first.number);
  assert.equal(flow.member.id, 1081);
  assert.equal(flow.applications.length, 2);
  assert.equal(first.number, 10017);
  assert.equal(second.number, 10018);
});

test("resume checkpoints and authorization failures preserve the persisted fach state", () => {
  const flow = new MembershipApplicationHarness();
  const application = flow.startSigning(flow.start());
  flow.sign(application, adultKeys.slice(0, 2));
  const checkpoint = flow.snapshot();
  const parsed = JSON.parse(checkpoint);
  assert.equal(parsed.applications[0].status, "zur_signatur");
  assert.equal(parsed.applications[0].signatures.length, 2);
  assert.throws(() => flow.startBoard(application, false), /forbidden/);
  assert.throws(() => flow.decide(application, "annehmen", "vorstandssitzung", false), /forbidden/);
  assert.equal(application.status, "zur_signatur");
  assert.equal(application.decision, "offen");
});

test("the published v5 field spec uses persistent signatures", () => {
  assert.equal(canStartMembershipApplication("aufgenommen", "none"), false);
  assert.equal(usesPersistentMembershipSignatures("preview", "mitgliedsantrag-field-spec/v5"), false);
  assert.equal(usesPersistentMembershipSignatures("finalize", "mitgliedsantrag-field-spec/v4"), true);
  assert.equal(usesPersistentMembershipSignatures("finalize", "mitgliedsantrag-field-spec/v5"), true);
});
