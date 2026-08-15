#!/usr/bin/env bash
# Runs once when the dev container / Codespace is created: restore the API, install the web client's
# dependencies, and trust the local HTTPS dev cert — so `dotnet run` (which auto-launches the client via
# SpaProxy) works on first try, in the cloud or locally.
set -e

echo "Restoring .NET dependencies…"
dotnet restore

# Install deps for whichever web client shipped. React scaffolds to "<name>.client", Angular to
# "<name>.angular.client" — both end in ".client", so one glob covers either flavour (only one exists).
for dir in *.client; do
  if [ -f "$dir/package.json" ]; then
    echo "Installing web client dependencies in $dir…"
    (cd "$dir" && npm install)
  fi
done

# Generate + trust the ASP.NET Core HTTPS development certificate (best-effort — harmless if already trusted).
dotnet dev-certs https || true

echo ""
echo "✅ Setup complete. Start the app with:"
echo "     dotnet run --project NetForge.Server"
echo "   The API launches the web client automatically (SpaProxy) — open the forwarded web app port."
