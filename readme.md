VERGUET Romain
# MonECC - Implémentation de Cryptographie sur Courbes Elliptiques

**MonECC** est une application console développée en .NET 10 (Native AOT) implémentant un cryptosystème hybride alliant la Cryptographie sur Courbes Elliptiques (ECC) pour l'échange de clés et AES-128 pour le chiffrement symétrique.

Ce projet a été réalisé dans le cadre du Mastère Architecture Logiciel, avec une attention particulière portée à la modularité, la testabilité et la performance, en suivant les principes de la **Clean Architecture**.

## Architecture Technique

Le projet est structuré selon une architecture en couches strictes (Onion/Clean Architecture) pour découpler la logique mathématique des détails d'implémentation système.

### Organisation de la Solution

1. **MonECC.Domain (Cœur)**

* Contient les entités mathématiques pures (`Point`, `Curve`) et l'arithmétique modulaire.
* Implémente les algorithmes ECC "from scratch" : Addition de points, Doublement de points, et Multiplication scalaire via l'algorithme **Double-and-Add** ($O(\log n)$).
* Définit les interfaces (`IFileSystem`, `ICryptoProvider`) pour l'inversion de dépendance.
* Aucune dépendance externe.

2. **MonECC.Application (Orchestration)**

* Contient la logique métier (Use Cases) sous forme de commandes : `KeyGenCommand`, `EncryptCommand`, `DecryptCommand`.
* Gère le protocole d'échange de clés (ECDH) et la dérivation de clés (SHA256).
* Intègre une validation robuste lors de la génération de clés pour éviter les points singuliers (points à l'infini ou d'ordre faible).

3. **MonECC.Infrastructure (Implémentation)**

* Implémente les interfaces du Domaine.
* Gère les Entrées/Sorties fichiers avec les formats spécifiques (`.pub`, `.priv`).
* Fournit les primitives cryptographiques système pour AES-CBC (PKCS7) et SHA256 via `System.Security.Cryptography`.

4. **MonECC.CLI (Présentation)**

* Point d'entrée de l'application.
* Gère le parsing des arguments et l'injection de dépendances.
* Configuré pour la compilation **Native AOT** (Ahead-of-Time) afin de produire un binaire autonome et léger.

## Détails Mathématiques & Algorithmes

Le projet respecte les contraintes académiques suivantes :

* **Courbe** : $y^2 = x^3 + 35x + 3 \pmod{101}$
* **Point Générateur** : $P(2, 9)$
* **Addition (P+Q)** : Utilise la formule de la pente $\lambda = (y_q - y_p)(x_q - x_p)^{-1} \pmod p$
* **Doublement (2P)** : Utilise la formule de la tangente $\lambda = (3x_p^2 + a)(2y_p)^{-1} \pmod p$
* **Protocole Hybride** :
1. Calcul du secret partagé  $S = k_{priv} \times Q_{pub}$
2. Hachage de la coordonnée $X$ du secret via SHA256.
3. Découpage du hash : 16 premiers octets pour l'IV, 16 suivants pour la clé AES.
4. Chiffrement AES-128 en mode CBC avec padding PKCS7.

## Prérequis

* **Docker** (Recommandé pour l'exécution et la compilation isolée).
* **SDK .NET 10** (Uniquement pour le développement local).

## Installation et Utilisation via Docker

L'utilisation de Docker est la méthode privilégiée car elle encapsule toutes les dépendances de compilation Native AOT.

### 1. Construction de l'image

Placez-vous à la racine de la solution et lancez :

```bash
docker build -t monecc .

```

### 2. Guide d'Utilisation Complet

Nous utilisons un volume (`-v`) pour permettre au conteneur de lire et écrire des fichiers sur votre machine hôte. Les fichiers seront générés dans le dossier courant (`/data` dans le conteneur).

#### A. Génération de clés (`keygen`)

Génère une paire de clés privée et publique. Le programme boucle automatiquement jusqu'à trouver une paire mathématiquement valide sur la courbe $F_{101}$.

**Syntaxe :** `keygen [-f filename] [-s size]`

* `-f <filename>` : Préfixe des fichiers (défaut : "monECC").
* `-s <size>` : Taille maximale de l'aléa pour la clé privée (défaut : 1000).

**Exemple :**

```bash
# Génère 'maCle.priv' et 'maCle.pub' avec un aléa jusqu'à 5000
docker run --rm -v ${PWD}:/data monecc keygen -f maCle -s 5000

```

#### B. Chiffrement (`crypt`)

Chiffre un message en utilisant la clé publique du destinataire.

**Syntaxe :** `crypt <pubKeyFile> [<message>] [-i input] [-o output]`

* `<pubKeyFile>` : Fichier de la clé publique du destinataire.
* `<message>` : Message texte (si l'option -i n'est pas utilisée).
* `-i <file>` : Lit le message depuis un fichier texte.
* `-o <file>` : Écrit le résultat chiffré dans un fichier au lieu de la console.

**Exemple (Console) :**

```bash
docker run --rm -v ${PWD}:/data monecc crypt maCle.pub "Message Secret"

```

**Exemple (Fichier vers Fichier) :**

```bash
# Chiffre le contenu de 'clair.txt' vers 'secret.enc'
docker run --rm -v ${PWD}:/data monecc crypt maCle.pub -i clair.txt -o secret.enc

```

#### C. Déchiffrement (`decrypt`)

Déchiffre un message en utilisant votre clé privée.

**Syntaxe :** `decrypt <privKeyFile> [<cipher>] [-i input] [-o output]`

* `<privKeyFile>` : Fichier de votre clé privée.
* `<cipher>` : Message chiffré en Base64 (si l'option -i n'est pas utilisée).
* `-i <file>` : Lit le message chiffré depuis un fichier.
* `-o <file>` : Écrit le message déchiffré dans un fichier.

**Exemple :**

```bash
# Déchiffre le fichier 'secret.enc' et affiche le résultat
docker run --rm -v ${PWD}:/data monecc decrypt maCle.priv -i secret.enc

```

*Note : Sous Linux/Mac, remplacez `${PWD}` par `$(pwd)`.*

## Tests Unitaires

Le projet inclut une suite de tests unitaires (xUnit) couvrant :

* L'arithmétique modulaire (inverses, modulos négatifs).
* Les opérations de courbe (appartenance, addition, multiplication).
* Le flux complet de chiffrement/déchiffrement via des Mocks système.

Pour lancer les tests (nécessite le SDK .NET localement) :

```bash
dotnet test

```

## Structure du Projet

```text
MonECC/
├── src/
│   ├── MonECC.Domain/          # Logique mathématique pure
│   ├── MonECC.Application/     # Cas d'utilisation et commandes
│   ├── MonECC.Infrastructure/  # Accès fichiers et crypto système
│   └── MonECC.CLI/             # Point d'entrée, Parser et AOT
├── tests/
│   └── MonECC.Tests/           # Tests unitaires
├── Dockerfile                  # Configuration Multi-stage build
└── MonECC.sln                  # Solution .NET

```