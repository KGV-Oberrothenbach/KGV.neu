using KGV.Core.Models;

namespace KGV.Maui.State;

public enum HomeDetailKind
{
    None,
    WorkAssignment,
    Appointment,
    Announcement
}

public sealed class HomeContextState
{
    public HomeDetailKind DetailKind { get; private set; }
    public HomeWorkAssignmentItem? WorkAssignment { get; private set; }
    public HomeAppointmentItem? Appointment { get; private set; }
    public HomeAnnouncementItem? Announcement { get; private set; }

    public void SetWorkAssignment(HomeWorkAssignmentItem item)
    {
        DetailKind = HomeDetailKind.WorkAssignment;
        WorkAssignment = item;
        Appointment = null;
        Announcement = null;
    }

    public void SetAppointment(HomeAppointmentItem item)
    {
        DetailKind = HomeDetailKind.Appointment;
        Appointment = item;
        WorkAssignment = null;
        Announcement = null;
    }

    public void SetAnnouncement(HomeAnnouncementItem item)
    {
        DetailKind = HomeDetailKind.Announcement;
        Announcement = item;
        WorkAssignment = null;
        Appointment = null;
    }

    public void Clear()
    {
        DetailKind = HomeDetailKind.None;
        WorkAssignment = null;
        Appointment = null;
        Announcement = null;
    }
}
