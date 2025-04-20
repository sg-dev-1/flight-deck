# FlightDeck Backend

This is the ASP.NET Core Web API backend for the FlightDeck real-time flight board management system. It provides API endpoints for managing flight information and uses SignalR to broadcast updates.

## Technologies Used

* .NET 8
* ASP.NET Core Web API
* C#
* SignalR (for real-time updates)
* Serilog (for logging to console and file)
* Swashbuckle (for Swagger UI API documentation)
* In-Memory Data Store (for flight data)

## Setup Instructions

1.  **Prerequisites:** Ensure you have the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
2.  **Clone Repository:** Clone this repository to your local machine.
3.  **Navigate to Project:** Open a terminal or command prompt and navigate to the main project directory (e.g., `cd path/to/repo/FlightDeck`).
4.  **Restore Dependencies:** Run `dotnet restore` to install necessary NuGet packages.
5.  **Run Application:** Run `dotnet run`. The application will start, typically listening on `http://localhost:5177` (check the console output for the exact URL).

## API Documentation (Swagger)

While the application is running in the `Development` environment, you can access the interactive Swagger UI documentation by navigating to `/swagger` in your browser (e.g., `http://localhost:5177/swagger`).

## API Endpoints

The following endpoints are available:

* **`GET /api/flights`**: Retrieves a list of all current flights.
    * Optional query parameters: `destination` (string), `status` (string - e.g., "Scheduled", "Boarding", "Departed").
* **`POST /api/flights`**: Adds a new flight. Requires a JSON body with `flightNumber`, `destination`, `departureTime` (future UTC DateTime string), and `gate`.
* **`DELETE /api/flights/{id}`**: Deletes a flight by its unique ID (Guid).
* **`GET /api/flights/{id}`**: Retrieves a single flight by its unique ID (Guid).

### SignalR Hub

* **`/flightHub`**: Clients connect to this endpoint using the SignalR protocol to receive real-time updates (`FlightAdded`, `FlightDeleted`).

## Example API Requests (cURL)

*(Replace `http://localhost:5177` if your application runs on a different port. Replace placeholder IDs like `{guid}` with actual IDs.)*

**1. Get All Flights:**

```bash
curl -X GET "http://localhost:5177/api/flights" -H "accept: application/json"
2. Get Filtered Flights (by Status):curl -X GET "http://localhost:5177/api/flights?status=Scheduled" -H "accept: application/json"
3. Add a New Flight:curl -X POST "http://localhost:5177/api/flights" -H "accept: application/json" -H "Content-Type: application/json" -d "{\"flightNumber\":\"AA100\",\"destination\":\"New York\",\"departureTime\":\"2025-12-01T10:00:00Z\",\"gate\":\"B5\"}"
