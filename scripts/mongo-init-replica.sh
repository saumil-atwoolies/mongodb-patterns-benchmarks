#!/bin/bash
# Initialize a single-node MongoDB replica set.
# This script is mounted into /docker-entrypoint-initdb.d/ and runs once
# after MongoDB starts for the first time.

# Wait for MongoDB to be ready to accept connections
until mongosh --quiet --eval "db.adminCommand('ping').ok" 2>/dev/null; do
  echo "Waiting for MongoDB to start..."
  sleep 2
done

echo "Initializing replica set rs0..."
mongosh --quiet --eval '
  rs.initiate({
    _id: "rs0",
    members: [{ _id: 0, host: "localhost:27017" }]
  })
'

# Wait for the replica set to elect a primary
until mongosh --quiet --eval "rs.status().myState === 1" 2>/dev/null; do
  echo "Waiting for replica set primary election..."
  sleep 2
done

echo "Replica set rs0 initialized successfully."
