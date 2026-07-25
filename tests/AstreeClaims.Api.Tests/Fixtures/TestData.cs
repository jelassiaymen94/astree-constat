using AstreeClaims.Api.Data;
using AstreeClaims.Api.Models;

namespace AstreeClaims.Api.Tests.Fixtures;

internal static class TestData
{
    public static void Seed(AstreeClaimsDbContext db)
    {
        if (db.Sinistres.Any())
        {
            return;
        }

        var client1 = new Client
        {
            ClientId = "CLI-001",
            Nom = "Ben Salah",
            Prenom = "Amine",
            Gouvernorat = "Tunis"
        };
        var client2 = new Client
        {
            ClientId = "CLI-002",
            Nom = "Trabelsi",
            Prenom = "Nour",
            Gouvernorat = "Sfax"
        };

        var contract1 = Contract("CTR-001", client1, "Tous risques");
        var contract2 = Contract("CTR-002", client1, "Tiers");
        var contract3 = Contract("CTR-003", client2, "Tous risques");
        var contract4 = Contract("CTR-004", client2, "Tiers");

        var vehicle1 = Vehicle("VEH-001", contract1, "Peugeot", "208", "111-TUN-1111");
        var vehicle2 = Vehicle("VEH-002", contract2, "Renault", "Clio", "222-TUN-2222");
        var vehicle3 = Vehicle("VEH-003", contract3, "Kia", "Rio", "333-TUN-3333");
        var vehicle4 = Vehicle("VEH-004", contract4, "Toyota", "Yaris", "444-TUN-4444");

        db.AddRange(client1, client2, contract1, contract2, contract3, contract4,
            vehicle1, vehicle2, vehicle3, vehicle4);
        db.AddRange(
            Claim("CLM-1001", client1, contract1, vehicle1, new DateOnly(2026, 6, 4), "Accident", "Choc avant", "Ouvert"),
            Claim("CLM-1002", client1, contract2, vehicle2, new DateOnly(2026, 5, 3), "Vol", "Vol du véhicule", "Clos"),
            Claim("CLM-2001", client2, contract3, vehicle3, new DateOnly(2026, 4, 2), "Accident", "Choc arrière", "Ouvert"),
            Claim("CLM-3001", client2, contract4, vehicle4, new DateOnly(2026, 3, 1), "Bris de glace", "Pare-brise", "En cours"));
        db.SaveChanges();
    }

    private static Contrat Contract(string id, Client client, string coverage) => new()
    {
        ContractId = id,
        ClientId = client.ClientId,
        Client = client,
        TypeCouverture = coverage,
        DateDebut = new DateOnly(2026, 1, 1),
        DateFin = new DateOnly(2026, 12, 31)
    };

    private static Vehicule Vehicle(
        string id, Contrat contract, string brand, string model, string registration) => new()
    {
        VehicleId = id,
        ContractId = contract.ContractId,
        Contract = contract,
        TypeVehicule = "Voiture",
        Marque = brand,
        Modele = model,
        Immatriculation = registration
    };

    private static Sinistre Claim(
        string id,
        Client client,
        Contrat contract,
        Vehicule vehicle,
        DateOnly date,
        string type,
        string description,
        string status) => new()
    {
        ClaimId = id,
        ClientId = client.ClientId,
        Client = client,
        ContractId = contract.ContractId,
        Contract = contract,
        VehicleId = vehicle.VehicleId,
        Vehicle = vehicle,
        DateSinistre = date,
        TypeSinistre = type,
        Description = description,
        MontantEstime = 2500m,
        MontantIndemnisation = 1800m,
        Statut = status
    };
}
