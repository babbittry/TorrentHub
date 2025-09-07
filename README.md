# Sakura.PT - Private Tracker Backend

This repository contains the backend source code for Sakura.PT, a modern private tracker website built with ASP.NET Core.

## Features

-   **User Management**: Secure user registration, login, and profile management.
-   **Torrent System**: Robust torrent uploading, downloading, and detailed listings. Includes features like sticky, freeleech, and double-upload.
-   **Community & Interaction**:
    -   Fully-featured forum for discussions.
    -   Torrent commenting system.
    -   User-driven polls.
    -   Private messaging between users.
    -   Announcement system for site-wide news.
-   **Economy & Store**:
    -   Users can earn "Coins" through seeding and other activities.
    -   A store to spend Coins on various items like badges, upload credit, etc.
-   **Request System**: Users can post requests for desired content and other users can fulfill them for a bounty.
-   **Search**: Fast and powerful torrent search powered by MeiliSearch.
-   **Administration**:
    -   Admin panel for user management, site settings, etc.
    -   Torrent and user reporting system.
    -   Site statistics and leaderboards.
-   **External Integration**: Automatically fetches movie/TV show metadata from TMDb.

## Future Development (Planned / To-Do)

-   Advanced user class and permission management.
-   Client blacklisting/whitelisting.
-   Comprehensive site logging for admin review.
-   Subtitle uploading and management for torrents.
-   A full suite of automated tests (Unit & Integration).
-   Bonus/Coupon system.

## Tech Stack

-   **Framework**: ASP.NET Core 9
-   **ORM**: Entity Framework Core
-   **Database**: PostgreSQL
-   **Caching**: Garnet
-   **Search**: MeiliSearch
-   **Authentication**: JWT over HTTP-only cookies
-   **Mapper**: Riok.Mapperly (Source Generator)

## Getting Started

Follow these instructions to get the project up and running on your local machine for development and testing purposes.

### Prerequisites

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [Docker](https://www.docker.com/products/docker-desktop) and Docker Compose

### Installation & Setup

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/Sakura.PT.git
    cd Sakura.PT
    ```

2.  **Start dependent services:**
    Run the following command from the root of the project to start PostgreSQL, Garnet, and MeiliSearch in Docker containers.
    ```bash
    docker-compose up -d
    ```

3.  **Configure the application:**
    Navigate to `src/TorrentHub` and copy `appsettings.Example.json` to `appsettings.Development.json`. Update the connection strings and other settings as needed to match the credentials in `docker-compose.yml`.

4.  **Run the application:**
    Navigate to the main Web API project and run the application.
    ```bash
    cd src/TorrentHub
    dotnet run
    ```
    The API will be available at `https://localhost:7122` and `http://localhost:5122`.

    On the first run in a `Development` environment, the application will automatically apply any pending Entity Framework Core migrations and seed the database with test data.

## Key Commands

All commands should be run from the `src/TorrentHub` directory.

-   **Run the API:**
    ```bash
    dotnet run
    ```

-   **Add a new database migration:**
    ```bash
    dotnet ef migrations add <MigrationName>
    ```

-   **Apply database migrations:**
    ```bash
    dotnet ef database update
    ```

## Project Structure

The solution is divided into three main projects:

-   `src/TorrentHub`: The main ASP.NET Core Web API project. This contains the controllers, service implementations, background tasks, and application entry point.
-   `src/TorrentHub.Core`: A core class library containing shared code, including database entities, DTOs (Data Transfer Objects), the `ApplicationDbContext`, and service interfaces.
-   `src/TorrentHub.Tracker`: A separate, lightweight tracker server responsible for handling announce requests.

## Acknowledgements / Open Source Libraries

This project utilizes several fantastic open-source libraries:

-   [ASP.NET Core](https://github.com/dotnet/aspnetcore)
-   [Entity Framework Core](https://github.com/dotnet/efcore)
-   [PostgreSQL](https://www.postgresql.org/)
-   [Garnet](https://github.com/microsoft/garnet)
-   [MeiliSearch](https://github.com/meilisearch/meilisearch)
-   [Riok.Mapperly](https://github.com/riok/mapperly)
-   [Serilog](https://github.com/serilog/serilog)
-   [MailKit](https://github.com/jstedfast/MailKit)
-   [TMDb](https://www.themoviedb.org/)
