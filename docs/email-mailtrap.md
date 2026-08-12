# Envoi d’e-mails avec Mailtrap Sandbox

Cette fonctionnalité est réservée à la démonstration. Le gestionnaire génère un courrier, le modifie dans l’éditeur WYSIWYG, le prévisualise puis confirme explicitement l’envoi. Mailtrap intercepte le message : aucun assuré réel ne le reçoit.

## 1. Configuration

Ajouter dans `.env` les valeurs SMTP fournies par Mailtrap Sandbox :

```dotenv
MAILTRAP_SMTP_HOST="..."
MAILTRAP_SMTP_PORT="2525"
MAILTRAP_SMTP_USERNAME="..."
MAILTRAP_SMTP_PASSWORD="..."
EMAIL_FROM_ADDRESS="sinistres-demo@astree.local"
EMAIL_FROM_NAME="ASTREE Assurances — Démonstration"
EMAIL_DEMO_MODE="true"
EMAIL_DEMO_RECIPIENT="demo@astree.local"
```

Ne jamais commiter le fichier `.env` ni afficher les identifiants pendant la présentation.

## 2. Mise à jour de la base existante

Exécuter une seule fois `database/upgrade-email.sql` dans SQL Server. Le script ajoute `Clients.Email` et crée `EmailLogs`. Il est idempotent.

Exemple avec l’outil SQL Server du conteneur :

```powershell
docker cp database/upgrade-email.sql astree-sqlserver:/tmp/upgrade-email.sql
docker exec -it astree-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$env:SQLSERVER_SA_PASSWORD" -C -i /tmp/upgrade-email.sql
```

Selon l’image SQL Server, le chemin peut être `/opt/mssql-tools/bin/sqlcmd`.

## 3. Démarrage

```powershell
.\start.cmd
.\frontend\start.cmd
```

Après avoir généré un brouillon, la section « Préparer l’e-mail à l’assuré » apparaît. L’adresse affichée est fictive. Si `EMAIL_DEMO_RECIPIENT` est renseigné, le backend redirige tous les messages vers cette adresse avant l’envoi SMTP.

## 4. Endpoints

```http
POST /api/claims/{claimId}/emails/send
GET  /api/claims/{claimId}/emails
```

Chaque envoi possède un `clientRequestId` unique pour limiter les doubles envois. Les statuts `pending`, `sent` et `failed` sont conservés dans `EmailLogs`.

## 5. Limites du prototype

- pas d’authentification ni de rôles ;
- pas de pièces jointes ;
- adresses clients fictives ;
- nettoyage HTML adapté au prototype, à remplacer par une bibliothèque auditée avant production ;
- Mailtrap Sandbox uniquement, jamais un relais de production.
