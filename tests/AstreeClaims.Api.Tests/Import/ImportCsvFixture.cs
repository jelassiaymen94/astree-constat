namespace AstreeClaims.Api.Tests.Import;

internal sealed class ImportCsvFixture : IDisposable
{
    public ImportCsvFixture()
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), $"astree-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
        WriteValidFiles();
    }

    public string DirectoryPath { get; }

    public void WriteValidFiles()
    {
        Write("clients.csv",
            "ClientId,Nom,Prenom,Gouvernorat\n" +
            "CLI-T01,Test,Client,Tunis\n");
        Write("contrats.csv",
            "ContractId,ClientId,TypeCouverture,DateDebut,DateFin\n" +
            "CTR-T01,CLI-T01,Tous risques,2026-01-01,2026-12-31\n");
        Write("vehicules.csv",
            "VehicleId,ContractId,TypeVehicule,Marque,Modele,Immatriculation\n" +
            "VEH-T01,CTR-T01,Voiture,Peugeot,208,111-TUN-1111\n");
        Write("sinistres.csv",
            "ClaimId,ContractId,ClientId,VehicleId,DateSinistre,TypeSinistre,Description,MontantEstime,MontantIndemnisation,Statut\n" +
            "CLM-T01,CTR-T01,CLI-T01,VEH-T01,2026-06-15,Accident,Choc avant,2500.00,1800.00,Ouvert\n");
    }

    public void Write(string fileName, string content) =>
        File.WriteAllText(Path.Combine(DirectoryPath, fileName), content);

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}
