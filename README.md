# TP n°1 – Culture DevOps et Conteneurisation avec Docker

## 1. Présentation du projet

Ce projet consiste à développer et conteneuriser une API REST de gestion d'un catalogue de produits à l'aide d'ASP.NET Core Minimal API, Entity Framework Core, Microsoft SQL Server, Docker et Docker Compose.

L'application permet de :

- Vérifier l'état de fonctionnement de l'API.
- Consulter la liste des produits.
- Ajouter de nouveaux produits.
- Enregistrer les données dans une base de données SQL Server.

### Environnement technique

- .NET 10 / ASP.NET Core Minimal API
- Entity Framework Core
- Microsoft SQL Server 2022
- Docker
- Docker Compose
- Swagger / OpenAPI

## 2. Structure du projet

```text
DevOps-TP1/
├── src/
│   └── ProductCatalog.Api/
│       ├── Data/
│       │   └── AppDbContext.cs
│       ├── Models/
│       │   └── Product.cs
│       ├── Program.cs
│       ├── appsettings.json
│       └── ProductCatalog.Api.csproj
├── .dockerignore
├── .env.example
├── .gitignore
├── Dockerfile
├── compose.yaml
└── README.md
```

## 3. Exécution de l'application

### Prérequis

- .NET SDK 10
- Docker Desktop
- Docker Compose

### Configuration de l'environnement

Créer un fichier `.env` à partir de `.env.example`, puis configurer le mot de passe du serveur SQL Server.

Ce fichier contient les variables d'environnement utilisées par Docker Compose :

- `DB_NAME` : nom de la base de données.
- `DB_SA_PASSWORD` : mot de passe du compte administrateur SQL Server.
- `API_PORT` : port d'accès à l'API depuis la machine hôte.

### Démarrage des conteneurs

Depuis le répertoire racine du projet, exécuter :

```powershell
docker compose up -d --build
```

Cette commande construit l'image de l'API et démarre les deux services en arrière-plan.

Vérifier l'état des conteneurs :

```powershell
docker compose ps
```

Consulter les journaux de l'API :

```powershell
docker compose logs -f api
```

### Accès à l'application

- **Swagger :** http://localhost:5000/swagger
- **Vérification de l'état de l'API :** http://localhost:5000/health
- **Liste des produits :** http://localhost:5000/api/products

### Tests des endpoints

Vérification de l'état de l'API :

```powershell
curl.exe http://localhost:5000/health
```

Récupération de la liste des produits :

```powershell
curl.exe http://localhost:5000/api/products
```

Ajout d'un produit :

```powershell
curl.exe -X POST http://localhost:5000/api/products `
  -H "Content-Type: application/json" `
  -d '{"name":"Keyboard","price":120}'
```

### Arrêt de l'application

```powershell
docker compose down
```

Cette commande arrête et supprime les conteneurs et le réseau créé par Docker Compose, mais conserve le volume de données.

Pour supprimer également le volume et les données persistées :

```powershell
docker compose down -v
```

## 4. Conteneurisation avec Docker

### 4.1. Dockerfile multi-stage

Le Dockerfile utilise une approche de construction multi-stage composée de plusieurs étapes :

1. **Base :** utilise l'image ASP.NET Runtime pour exécuter l'application.
2. **Build :** utilise l'image .NET SDK pour restaurer les dépendances et compiler le code source.
3. **Publish :** publie l'application en mode Release.
4. **Final :** copie uniquement les fichiers publiés dans l'image finale basée sur ASP.NET Runtime.

Cette approche permet de séparer l'environnement de développement et de compilation de l'environnement d'exécution.

Ainsi, l'image finale ne contient pas le SDK, le compilateur ni les fichiers intermédiaires de compilation.

Cela permet de réduire la taille de l'image, les ressources nécessaires à son transfert et sa surface d'attaque.

### 4.2. Exécution avec un utilisateur non privilégié

Le Dockerfile contient l'instruction :

```dockerfile
USER app
```

Cette instruction permet d'exécuter l'application avec un utilisateur non privilégié plutôt qu'avec le compte administrateur `root`.

Cette pratique respecte le principe du moindre privilège et limite les conséquences potentielles d'une compromission de l'application.

### 4.3. Réseau Docker

L'API et SQL Server communiquent à travers un réseau Docker de type bridge.

L'API utilise le nom du service `db` pour accéder au serveur SQL Server sur le port `1433`.

Cette configuration permet aux conteneurs de communiquer directement entre eux sans utiliser l'adresse IP de la machine hôte.

### 4.4. Persistance des données

Un volume Docker nommé est associé au répertoire :

```text
/var/opt/mssql
```

Ce volume permet de conserver les données de SQL Server même après la suppression et la recréation du conteneur.

Les données restent disponibles tant que le volume n'est pas supprimé.

## 5. Orchestration avec Docker Compose

Docker Compose permet de définir et de gérer plusieurs services à partir d'un seul fichier de configuration.

Dans ce projet, le fichier `compose.yaml` définit deux services principaux :

- **db :** conteneur SQL Server 2022.
- **api :** conteneur de l'application ASP.NET Core.

La configuration comprend également :

- Des variables d'environnement pour la configuration de la base de données.
- Un volume nommé pour assurer la persistance des données.
- Un réseau Docker partagé entre les services.
- Un healthcheck pour vérifier la disponibilité de SQL Server.
- Une dépendance entre l'API et la base de données.

La condition `service_healthy` de `depends_on` permet de démarrer l'API uniquement lorsque le service de base de données est déclaré sain par Docker.

Le healthcheck de SQL Server exécute une requête SQL à l'aide de `sqlcmd` afin de vérifier que le serveur est opérationnel.

## 6. Surveillance des ressources avec Docker

La surveillance des ressources a été effectuée avec la commande :

```powershell
docker stats
```

Les mesures suivantes ont été relevées pendant l'exécution des conteneurs :

| Indicateur | catalog-api | catalog-db |
|---|---:|---:|
| Utilisation CPU | 0.02 % | 6.18 % |
| Mémoire utilisée | 56.34 MiB | 1.007 GiB |
| Pourcentage mémoire | 1.47 % | 26.88 % |
| Entrées / sorties réseau | 18 kB / 19.3 kB | 661 kB / 111 kB |
| Entrées / sorties disque | 6.07 MB / 0 B | 300 MB / 118 MB |
| Nombre de processus | 24 | 220 |

Ces résultats montrent que l'API consomme relativement peu de ressources lorsqu'elle est inactive, tandis que SQL Server nécessite davantage de mémoire et de processus en arrière-plan.

Il s'agit de mesures ponctuelles réalisées pendant les tests. Les valeurs peuvent varier selon la charge de travail.

## 7. Questions et discussions

### 7.1. Pourquoi est-il important de surveiller la consommation des ressources dans un environnement DevOps ?

La surveillance des ressources permet d'identifier les problèmes de performance et les éventuels goulots d'étranglement.

Elle permet notamment de :

- Détecter une consommation excessive du processeur ou de la mémoire.
- Identifier les anomalies de fonctionnement.
- Optimiser l'allocation des ressources.
- Vérifier la stabilité des services.
- Anticiper les besoins en capacité et en montée en charge.

### 7.2. Quels sont trois indicateurs importants dans une démarche DevOps ?

Trois indicateurs importants sont :

**1. Deployment Frequency (Fréquence de déploiement)**

Cet indicateur mesure la fréquence à laquelle les nouvelles versions ou modifications de l'application sont déployées en production.

**2. Lead Time for Changes (Délai de mise en production)**

Il mesure le temps nécessaire entre la réalisation d'une modification du code et son déploiement en production.

**3. Change Failure Rate (Taux d'échec des changements)**

Il représente le pourcentage de déploiements qui entraînent un incident ou nécessitent une intervention corrective ou un retour à une version précédente.

Ces indicateurs permettent d'évaluer la rapidité de livraison, la fiabilité et l'efficacité du processus de développement.

### 7.3. Pourquoi utiliser un healthcheck applicatif plutôt que vérifier uniquement si un port est ouvert ?

La vérification d'un port ouvert confirme uniquement qu'un processus écoute sur ce port.

Elle ne garantit pas que l'application fonctionne correctement ni que ses dépendances sont disponibles.

Un healthcheck applicatif permet de vérifier que le service répond correctement. Il peut également être étendu pour contrôler des dépendances telles que la base de données.

Dans ce projet :

- L'API expose un endpoint `/health`.
- SQL Server utilise un healthcheck qui exécute une requête SQL.
- Docker Compose attend que la base de données soit déclarée saine avant de démarrer l'API.

Cette approche améliore la fiabilité du démarrage des services.

### 7.4. Quel est l'avantage d'un Dockerfile multi-stage ?

Un Dockerfile multi-stage permet de séparer l'environnement de compilation de l'environnement d'exécution.

L'image .NET SDK contient les outils nécessaires à la restauration des dépendances et à la compilation de l'application. Ces outils ne sont cependant pas nécessaires pour exécuter l'application publiée.

Grâce au multi-stage build, seuls les fichiers publiés sont copiés dans l'image finale.

Les principaux avantages sont :

- Réduction de la taille de l'image finale.
- Réduction du temps de transfert et de déploiement.
- Diminution de la surface d'attaque.
- Séparation claire entre compilation et exécution.

### 7.5. Pourquoi est-il préférable de ne pas exécuter un conteneur en tant que root ?

L'utilisateur `root` dispose de privilèges élevés dans le conteneur.

L'exécution avec un utilisateur non privilégié respecte le principe du moindre privilège.

En cas de compromission de l'application, les possibilités d'action de l'attaquant sont limitées par les permissions de cet utilisateur.

Cela contribue à renforcer la sécurité de l'environnement conteneurisé.

### 7.6. Comparaison entre l'image finale et l'image .NET SDK

Les tailles suivantes ont été relevées localement avec Docker :

| Image Docker | Taille disque |
|---|---:|
| .NET SDK 10.0 | 1.33 GB |
| ASP.NET Runtime 10.0 | 364 MB |
| Image finale Product Catalog API | 394 MB |

L'image SDK est environ 3,4 fois plus volumineuse que l'image finale de l'API.

Cette différence s'explique par le fait que l'image SDK contient les outils de développement et de compilation, alors que l'image finale contient uniquement le runtime ASP.NET et les fichiers publiés de l'application.

Le multi-stage build permet donc d'obtenir une image finale plus légère et mieux adaptée au déploiement.

## 8. Conclusion

Ce travail pratique a permis de découvrir et de mettre en œuvre les principes fondamentaux de la conteneurisation avec Docker.

La réalisation a couvert la création d'une API ASP.NET Core, la construction d'une image Docker, la communication entre conteneurs, l'isolation des bases de données, la persistance des données avec les volumes Docker et l'orchestration des services avec Docker Compose.

La surveillance des ressources et l'utilisation de healthchecks ont également permis de mieux comprendre les aspects liés à la performance, à la disponibilité et à la fiabilité d'une application conteneurisée.

Le projet final propose ainsi un environnement reproductible, organisé et adapté au déploiement d'une application composée de plusieurs services.