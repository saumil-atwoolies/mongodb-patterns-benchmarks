#!/bin/bash
# Custom entrypoint that starts mongod with replica set and initializes it.

# Start mongod in background
mongod --replSet rs0 --bind_ip_all &
MONGOD_PID=$!

# Wait for mongod to accept connections
until mongosh --quiet --eval "db.adminCommand('ping').ok" 2>/dev/null; do
  sleep 1
done

# Initialize replica set idempotently
mongosh --quiet --eval '
  try {
    var status = rs.status();
    if (status.ok) { print("Replica set already initialized"); }
  } catch(e) {
    rs.initiate({ _id: "rs0", members: [{ _id: 0, host: "mongodb:27017" }] });
    print("Replica set rs0 initiated");
  }
'

# Wait for mongod process (keeps container running)
wait $MONGOD_PID
