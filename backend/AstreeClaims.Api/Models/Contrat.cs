using System;
using System.Collections.Generic;

namespace AstreeClaims.Api.Models;

public partial class Contrat
{
    public string ContractId { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string TypeCouverture { get; set; } = null!;

    public DateOnly DateDebut { get; set; }

    public DateOnly DateFin { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<Sinistre> Sinistres { get; set; } = new List<Sinistre>();

    public virtual Vehicule? Vehicule { get; set; }
}
