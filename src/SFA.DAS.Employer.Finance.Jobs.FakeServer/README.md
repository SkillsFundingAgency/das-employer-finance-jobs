# Fake Server for Local Testing

This is a simple fake server that provides test endpoints for the ImportPayments durable function.

## Endpoints

- `GET /api/periodends` - Provider Payment API endpoint (returns test payment period ends)
- `GET /api/period-ends` - Finance API endpoint (returns existing period ends from finance)
- `GET /health` - Health check endpoint

## Running the Fake Server

```bash
cd src/SFA.DAS.Employer.Finance.Jobs.FakeServer
dotnet run
```

The server will start on `http://localhost:5001`

## Test Data

The fake server returns:
- **Provider Payment API** (`/api/periodends`): Returns 4 period ends:
  - `2324-R06` - January 2024
  - `2324-R07` - February 2024
  - `2324-R08` - March 2024
  - `2425-R01` - August 2024
- **Finance API** (`/api/period-ends`): Returns 2 existing period ends:
  - `2324-R06` - Already exists
  - `2324-R07` - Already exists

This means `2324-R08` and `2425-R01` will be identified as "new" period ends that need processing.

**Period End ID Format**: Period end IDs follow the format `YYY1YYY2-R##` where:
- `YYY1YYY2` represents the financial year (e.g., `2324` = 2023-2024, `2425` = 2024-2025)
- `R##` represents the period number (R01-R14)

## Modifying Test Data

Edit `Program.cs` to change the test data returned by the endpoints.


