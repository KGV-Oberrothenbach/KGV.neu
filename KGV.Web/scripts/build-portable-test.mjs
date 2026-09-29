import { execFileSync } from "node:child_process";
import {
  copyFileSync,
  cpSync,
  existsSync,
  mkdirSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const projectRoot = fileURLToPath(new URL("../", import.meta.url));
const distRoot = path.join(projectRoot, "dist");
const outputRoot = path.join(projectRoot, "outputs", "KGV-BrowserApp-Test");
const payloadRoot = path.join(projectRoot, "outputs", ".portable-payload");
const appRoot = path.join(payloadRoot, "app");
const runtimeRoot = path.join(payloadRoot, "runtime");
const payloadZip = path.join(outputRoot, "KGV-BrowserApp-Inhalt.zip");

if (!existsSync(path.join(distRoot, "server", "BUILD_ID"))) {
  throw new Error("Der Produktions-Build fehlt. Zuerst npm run build ausfuehren.");
}

rmSync(outputRoot, { recursive: true, force: true });
rmSync(payloadRoot, { recursive: true, force: true });
mkdirSync(appRoot, { recursive: true });
mkdirSync(runtimeRoot, { recursive: true });
mkdirSync(outputRoot, { recursive: true });

cpSync(distRoot, path.join(appRoot, "dist"), { recursive: true });
copyFileSync(process.execPath, path.join(runtimeRoot, "node.exe"));

const runtimePackage = {
  name: "kgv-browserapp-portable-runtime",
  version: "0.1.0",
  private: true,
  type: "module",
  dependencies: {
    "@vitejs/plugin-react": "6.0.2",
    "@vitejs/plugin-rsc": "0.5.26",
    react: "19.2.6",
    "react-dom": "19.2.6",
    "react-server-dom-webpack": "19.2.6",
    vinext: "1.0.0-beta.5",
    vite: "8.0.13",
  },
};

writeFileSync(
  path.join(appRoot, "package.json"),
  `${JSON.stringify(runtimePackage, null, 2)}\n`,
  "utf8",
);

cpSync(path.join(projectRoot, "node_modules"), path.join(appRoot, "node_modules"), {
  recursive: true,
});

const sourceLock = JSON.parse(
  readFileSync(path.join(projectRoot, "package-lock.json"), "utf8"),
);
sourceLock.name = runtimePackage.name;
sourceLock.version = runtimePackage.version;
sourceLock.packages[""].name = runtimePackage.name;
sourceLock.packages[""].version = runtimePackage.version;
sourceLock.packages[""].dependencies = runtimePackage.dependencies;
delete sourceLock.packages[""].devDependencies;
writeFileSync(
  path.join(appRoot, "package-lock.json"),
  `${JSON.stringify(sourceLock, null, 2)}\n`,
  "utf8",
);

const npmCli = path.join(path.dirname(process.execPath), "node_modules", "npm", "bin", "npm-cli.js");
if (!existsSync(npmCli)) {
  throw new Error(`Die npm-Laufzeit wurde nicht gefunden: ${npmCli}`);
}
execFileSync(
  process.execPath,
  [npmCli, "prune", "--omit=dev", "--no-audit", "--no-fund", "--offline"],
  { cwd: appRoot, stdio: "inherit" },
);

const buildId = readFileSync(path.join(distRoot, "server", "BUILD_ID"), "utf8").trim();
writeFileSync(path.join(payloadRoot, "VERSION.txt"), `${buildId}\n`, "utf8");
writeFileSync(path.join(outputRoot, "VERSION.txt"), `${buildId}\n`, "utf8");
writeFileSync(
  path.join(runtimeRoot, "NODE-LICENSE.txt"),
  `Copyright Node.js contributors. All rights reserved.\n\nPermission is hereby granted, free of charge, to any person obtaining a copy\nof this software and associated documentation files (the \"Software\"), to deal\nin the Software without restriction, including without limitation the rights\nto use, copy, modify, merge, publish, distribute, sublicense, and/or sell\ncopies of the Software, and to permit persons to whom the Software is\nfurnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in\nall copies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\nIMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\nFITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\nAUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\nLIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\nOUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN\nTHE SOFTWARE.\n`,
  "utf8",
);

const launcher = `@echo off
setlocal
set "PAKET=%~dp0KGV-BrowserApp-Inhalt.zip"
set "VERSIONSDATEI=%~dp0VERSION.txt"

if not exist "%PAKET%" goto :paket_fehlt
if not exist "%VERSIONSDATEI%" goto :paket_fehlt
set /p VERSION=<"%VERSIONSDATEI%"
set "ZIEL=%LOCALAPPDATA%\\KGV-BrowserApp-Test\\%VERSION%"

curl.exe --silent --fail --max-time 1 "http://127.0.0.1:5173/" >nul 2>nul
if not errorlevel 1 (
  start "" "http://127.0.0.1:5173/"
  exit /b 0
)

if not exist "%ZIEL%\\bereit.txt" (
  echo Die KGV BrowserApp wird beim ersten Start lokal vorbereitet.
  if exist "%ZIEL%" rmdir /s /q "%ZIEL%"
  mkdir "%ZIEL%"
  tar.exe -xf "%PAKET%" -C "%ZIEL%"
  if errorlevel 1 (
    echo Das Testpaket konnte nicht entpackt werden.
    pause
    exit /b 1
  )
  echo bereit>"%ZIEL%\\bereit.txt"
)

start "KGV BrowserApp Server" /D "%ZIEL%\\app" "%ZIEL%\\runtime\\node.exe" "%ZIEL%\\app\\node_modules\\vinext\\dist\\cli.js" start --hostname 127.0.0.1 --port 5173
timeout /t 3 /nobreak >nul
start "" "http://127.0.0.1:5173/"
exit /b 0

:paket_fehlt
  echo Das KGV-BrowserApp-Testpaket ist unvollstaendig.
  pause
  exit /b 1
`;

writeFileSync(path.join(outputRoot, "KGV BrowserApp starten.cmd"), launcher, "utf8");

execFileSync("tar.exe", ["-a", "-c", "-f", payloadZip, "-C", payloadRoot, "."], {
  stdio: "inherit",
});

rmSync(payloadRoot, { recursive: true, force: true });
console.log(`\nPortables Testpaket erstellt:\n${outputRoot}`);
