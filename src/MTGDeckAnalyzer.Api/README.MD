Backend: cd MTGDeckAnalyzer.Api && dotnet run
Frontend: cd src/mtg-deck-analyzer-ui && npm run dev
Open http://localhost:5173 — the Vite proxy forwards /api calls to the .NET backend on port 5187.