using AstreeClaims.Api.Models;
using AstreeClaims.Api.Services.Import;
using AstreeClaims.Api.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AstreeClaims.Api.Tests.Import;

public sealed class DataImportServiceTests
{
    [Fact]
    public async Task First_import_inserts_the_complete_dataset()
    {
        await using var context = await SqliteTestContext.CreateAsync(seed: false);
        using var csv = new ImportCsvFixture();
        var service = CreateService(context);

        var result = await service.ImportAsync(csv.DirectoryPath);

        Assert.Equal(1, result.Clients.InsertedRows);
        Assert.Equal(1, result.Contrats.InsertedRows);
        Assert.Equal(1, result.Vehicules.InsertedRows);
        Assert.Equal(1, result.Sinistres.InsertedRows);
        Assert.Equal(1, await context.Db.Sinistres.CountAsync());
    }

    [Fact]
    public async Task Second_import_is_idempotent()
    {
        await using var context = await SqliteTestContext.CreateAsync(seed: false);
        using var csv = new ImportCsvFixture();
        var service = CreateService(context);
        await service.ImportAsync(csv.DirectoryPath);

        var result = await service.ImportAsync(csv.DirectoryPath);

        Assert.Equal(0, result.Clients.InsertedRows);
        Assert.Equal(0, result.Contrats.InsertedRows);
        Assert.Equal(0, result.Vehicules.InsertedRows);
        Assert.Equal(0, result.Sinistres.InsertedRows);
    }

    [Fact]
    public async Task Import_rejects_a_claim_outside_contract_period()
    {
        await using var context = await SqliteTestContext.CreateAsync(seed: false);
        using var csv = new ImportCsvFixture();
        csv.Write("sinistres.csv",
            "ClaimId,ContractId,ClientId,VehicleId,DateSinistre,TypeSinistre,Description,MontantEstime,MontantIndemnisation,Statut\n" +
            "CLM-T01,CTR-T01,CLI-T01,VEH-T01,2027-01-01,Accident,Choc,2500.00,1800.00,Ouvert\n");
        var service = CreateService(context);

        var error = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ImportAsync(csv.DirectoryPath));

        Assert.Contains("hors de la période contractuelle", error.Message);
        Assert.Empty(context.Db.Sinistres);
    }

    [Fact]
    public async Task Import_rejects_a_broken_relation()
    {
        await using var context = await SqliteTestContext.CreateAsync(seed: false);
        using var csv = new ImportCsvFixture();
        csv.Write("sinistres.csv",
            "ClaimId,ContractId,ClientId,VehicleId,DateSinistre,TypeSinistre,Description,MontantEstime,MontantIndemnisation,Statut\n" +
            "CLM-T01,CTR-UNKNOWN,CLI-T01,VEH-T01,2026-06-15,Accident,Choc,2500.00,1800.00,Ouvert\n");
        var service = CreateService(context);

        var error = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ImportAsync(csv.DirectoryPath));

        Assert.Contains("contrat absent", error.Message);
        Assert.Empty(context.Db.Sinistres);
    }

    [Fact]
    public async Task Database_failure_rolls_back_all_changes()
    {
        await using var context = await SqliteTestContext.CreateAsync(seed: false);
        SeedExistingContractAndVehicle(context);
        using var csv = new ImportCsvFixture();
        csv.Write("clients.csv",
            "ClientId,Nom,Prenom,Gouvernorat\n" +
            "CLI-EXIST,Existing,Client,Tunis\n" +
            "CLI-NEW,New,Client,Sfax\n");
        csv.Write("contrats.csv",
            "ContractId,ClientId,TypeCouverture,DateDebut,DateFin\n" +
            "CTR-EXIST,CLI-EXIST,Tiers,2026-01-01,2026-12-31\n" +
            "CTR-NEW,CLI-NEW,Tous risques,2026-01-01,2026-12-31\n");
        csv.Write("vehicules.csv",
            "VehicleId,ContractId,TypeVehicule,Marque,Modele,Immatriculation\n" +
            "VEH-CONFLICT,CTR-EXIST,Voiture,Kia,Rio,222-TUN-2222\n" +
            "VEH-NEW,CTR-NEW,Voiture,Toyota,Yaris,333-TUN-3333\n");
        csv.Write("sinistres.csv",
            "ClaimId,ContractId,ClientId,VehicleId,DateSinistre,TypeSinistre,Description,MontantEstime,MontantIndemnisation,Statut\n" +
            "CLM-NEW,CTR-NEW,CLI-NEW,VEH-NEW,2026-05-01,Accident,Choc,1000.00,500.00,Ouvert\n");
        var service = CreateService(context);

        await Assert.ThrowsAsync<DbUpdateException>(() => service.ImportAsync(csv.DirectoryPath));
        context.Db.ChangeTracker.Clear();

        Assert.False(await context.Db.Clients.AnyAsync(x => x.ClientId == "CLI-NEW"));
        Assert.False(await context.Db.Contrats.AnyAsync(x => x.ContractId == "CTR-NEW"));
        Assert.False(await context.Db.Vehicules.AnyAsync(x => x.VehicleId == "VEH-NEW"));
    }

    private static DataImportService CreateService(SqliteTestContext context) =>
        new(context.Db, NullLogger<DataImportService>.Instance);

    private static void SeedExistingContractAndVehicle(SqliteTestContext context)
    {
        var client = new Client
        {
            ClientId = "CLI-EXIST", Nom = "Existing", Prenom = "Client", Gouvernorat = "Tunis"
        };
        var contract = new Contrat
        {
            ContractId = "CTR-EXIST", ClientId = client.ClientId, Client = client,
            TypeCouverture = "Tiers", DateDebut = new DateOnly(2026, 1, 1), DateFin = new DateOnly(2026, 12, 31)
        };
        var vehicle = new Vehicule
        {
            VehicleId = "VEH-EXIST", ContractId = contract.ContractId, Contract = contract,
            TypeVehicule = "Voiture", Marque = "Renault", Modele = "Clio", Immatriculation = "111-TUN-1111"
        };
        context.Db.AddRange(client, contract, vehicle);
        context.Db.SaveChanges();
    }
}
