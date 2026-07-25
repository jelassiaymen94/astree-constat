# Dictionnaire des données

## Périmètre

Source unique : `donnees_assurance_tunisie2.xlsx`. Les autres jeux de données ne sont pas fusionnés.

## Clients

| Excel | CSV / SQL | Type SQL | Obligatoire |
|---|---|---|---|
| `Client_ID` | `ClientId` | `NVARCHAR(20)` | Oui |
| `Nom` | `Nom` | `NVARCHAR(100)` | Oui |
| `Prénom` | `Prenom` | `NVARCHAR(100)` | Oui |
| `Gouvernorat` | `Gouvernorat` | `NVARCHAR(100)` | Oui |

Les colonnes de profil, risque, revenu et comportement ne sont pas importées en S3.

## Contrats

| Excel | CSV / SQL | Type SQL | Obligatoire |
|---|---|---|---|
| `Contract_ID` | `ContractId` | `NVARCHAR(20)` | Oui |
| `Client_ID` | `ClientId` | `NVARCHAR(20)` | Oui |
| `Type_Couverture` | `TypeCouverture` | `NVARCHAR(100)` | Oui |
| `Date_Debut_Contrat` | `DateDebut` | `DATE` | Oui |
| `Date_Fin_Contrat` | `DateFin` | `DATE` | Oui |

Règle : `DateFin >= DateDebut`.

## Véhicules

| Excel | CSV / SQL | Type SQL | Obligatoire |
|---|---|---|---|
| `Vehicle_ID` | `VehicleId` | `NVARCHAR(20)` | Oui |
| `Contract_ID` | `ContractId` | `NVARCHAR(20)` | Oui |
| `Type_Vehicule` | `TypeVehicule` | `NVARCHAR(50)` | Oui |
| `Marque` | `Marque` | `NVARCHAR(50)` | Oui |
| `Modele` | `Modele` | `NVARCHAR(100)` | Oui |
| `Immatriculation` | `Immatriculation` | `NVARCHAR(30)` | Oui |

Un contrat possède au maximum un véhicule dans le schéma S3.

## Sinistres

| Excel | CSV / SQL | Type SQL | Obligatoire |
|---|---|---|---|
| `Claim_ID` | `ClaimId` | `NVARCHAR(20)` | Oui |
| `Contract_ID` | `ContractId` | `NVARCHAR(20)` | Oui |
| `Client_ID` | `ClientId` | `NVARCHAR(20)` | Oui |
| `Vehicle_ID` | `VehicleId` | `NVARCHAR(20)` | Oui |
| `Date_Sinistre_Claim` | `DateSinistre` | `DATE` | Oui |
| `Type_Sinistre_Claim` | `TypeSinistre` | `NVARCHAR(100)` | Oui |
| `Description_Sinistre_Claim` | `Description` | `NVARCHAR(MAX)` | Oui |
| `Montant_Estime_Dommage_Claim` | `MontantEstime` | `DECIMAL(18,2)` | Oui |
| `Montant_Indemnisation_Claim` | `MontantIndemnisation` | `DECIMAL(18,2)` | Oui |
| `Statut_Sinistre_Claim` | `Statut` | `NVARCHAR(50)` | Oui |

Colonnes non importées : `Est_Frauduleux_Claim`, `Incoherence_Dommages`, `Nature_Sinistre_Consistante`.

Règles : montants positifs ou nuls et date du sinistre comprise dans la période contractuelle.

## Valeurs observées

### Statuts valides

| Statut | Nombre |
|---|---:|
| `Clos_avec_indemnisation` | 1 105 |
| `Ouvert` | 996 |
| `En_cours_d_expertise` | 848 |
| `Clos_sans_indemnisation` | 303 |
| `Refusé` | 208 |

### Types valides

| Type | Nombre |
|---|---:|
| `Accident` | 1 878 |
| `Responsabilité civile` | 750 |
| `Bris de glace` | 445 |
| `Catastrophe naturelle` | 167 |
| `Vol` | 153 |
| `Incendie` | 67 |

## GenerationLogs

Table réservée à S4 : identifiant, sinistre, type de génération, instruction, contenu, modèle, version du prompt, succès, erreur, date et durée. Aucun contenu LLM n’est généré en S3.
