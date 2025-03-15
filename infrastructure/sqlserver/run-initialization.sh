#!/bin/bash

echo "Initializing SQL Server - waiting 15s"

# Wait to be sure that SQL Server came up
sleep 15s

echo "Trying to initialize database"

# Run the setup script to create the DB and the schema in the DB
# Note: make sure that your password matches what is in the Dockerfile
# NOTE: There's a quirk according to this: https://github.com/microsoft/mssql-docker/issues/892

if [ -d "/opt/mssql-tools18" ]; then
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $MSSQL_SA_PASSWORD -d master -i create-database.sql
else
    /opt/mssql-tools/bin/sqlcmd -C -S localhost -U sa -P $MSSQL_SA_PASSWORD -d master -i create-database.sql
fi
