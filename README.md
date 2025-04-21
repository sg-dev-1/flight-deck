# FlightDeck Backend

This is the ASP.NET Core Web API backend for the FlightDeck real-time flight board management system. It provides API endpoints for managing flight information and uses SignalR for real-time broadcasting of flight additions, deletions, and status changes. A background service actively monitors and pushes status updates.

## Technologies Used

* .NET 8
* ASP.NET Core Web API
* C#
* SignalR (for real-time updates)
* Serilog (for logging, configured via `appsettings.json` to console and file)
* Swashbuckle (for Swagger UI API documentation)
* In-Memory Data Store (using `ConcurrentDictionary` for flight data)
* ASP.NET Core BackgroundService (for periodic status checks)

## Setup Instructions

1.  **Prerequisites:** Ensure you have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
2.  **Clone Repository:** Clone this repository to your local machine.
3.  **Navigate to Project:** Open a terminal or command prompt and navigate to the main project directory (e.g., `cd path/to/repo/flight-deck`).
4.  **Restore Dependencies:** Run `dotnet restore` to install necessary NuGet packages (including Serilog packages, Swashbuckle, etc.).
5.  **Configure Logging (Optional):** Review and adjust the `"Serilog"` section in `appsettings.json` if needed (especially the file path in the "File" sink args if the default needs changing). Ensure the application has write permissions to the specified `Logs` directory.
6.  **Run Application:** Run `dotnet run`. The application will start, typically listening on `http://localhost:5177` (check the console output for the exact URL).

## API Documentation (Swagger)

While the application is running in the `Development` environment, you can access the interactive Swagger UI documentation by navigating to `/swagger` in your browser (e.g., `http://localhost:5177/swagger`).

## API Endpoints

The following endpoints are available:

* **`GET /api/flights`**: Retrieves a list of all current flights.
    * Optional query parameters:
        * `destination` (string): Filters flights containing the provided text in their destination (case-insensitive).
        * `status` (string): Filters flights based on their *dynamically calculated* status at the time of the request (e.g., "Scheduled", "Boarding", "Departed", "Landed", "Delayed"). Status calculation logic matches the client-side requirements.
* **`POST /api/flights`**: Adds a new flight.
    * Requires a JSON body with `flightNumber` (string, unique), `destination` (string), `departureTime` (future UTC DateTime string), and `gate` (string).
    * Performs server-side validation (required fields, future departure time, unique flight number).
    * On success, broadcasts the newly added `Flight` object via SignalR (`FlightAdded` message).
* **`DELETE /api/flights/{id}`**: Deletes a flight by its unique ID (Guid).
    * On success, broadcasts the *entire deleted* `Flight` object via SignalR (`FlightDeleted` message).
* **`GET /api/flights/{id}`**: Retrieves a single flight by its unique ID (Guid). Returns 404 if not found.

## Real-Time Features (SignalR)

* **Hub Endpoint**: `/flightHub`
* Clients connect to this endpoint using the SignalR protocol.
* **Server-to-Client Messages**:
    * `FlightAdded`: Broadcasts the full `Flight` object when a new flight is successfully added via the API.
    * `FlightDeleted`: Broadcasts the full `Flight` object of the flight that was successfully deleted via the API.
    * `FlightStatusChanged`: Broadcasts an object `{ flightId: "guid", newStatus: "string" }` when the background service detects a change in a flight's calculated status (checked approximately every minute).

## Background Service

* A `FlightStatusMonitorService` runs periodically (approx. every minute) in the background.
* It fetches current flights, calculates their potential status based on the defined logic, and compares it to the last known status.
* If a status change is detected, it triggers the `FlightStatusChanged` SignalR broadcast.

## Example API Requests (cURL)

**1. Get All Flights:**

```bash
curl -X GET "http://localhost:5177/api/flights" -H "accept: application/json"
