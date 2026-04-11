using System;

namespace KGV.Core.Models
{
    public sealed class MitgliedsantragDokumentRequest
    {
        public int MitgliedId { get; set; }
        public DateTime BeginnDatum { get; set; }
        public decimal Mitgliedsbeitrag { get; set; }
        public string? Status { get; set; }
    }
}
