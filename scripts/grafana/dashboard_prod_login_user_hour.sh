#!/bin/bash

# Mainnet
saveIndexUrl="http://10.10.33.179:9200/dashboardprodhourloginuserindex"

# Mainnet
userVisitUrl="http://10.10.33.179:9200/tomorrowdaoserver.userindex/_count"

number=1
# 1 hour
timeSeconds=3600

# Check if the index exists
response_code=$(curl -s -o /dev/null -w "%{http_code}" -I "$saveIndexUrl")

if [ "$response_code" = "200" ]; then
    echo "Index '$saveIndexUrl' exists."
else
    echo "Index '$saveIndexUrl' does not exist."
    # Create index request body
    create_request='{
        "mappings": {
            "properties": {
                "id": {
                    "type": "keyword"
                },
                "createTime": {
                    "type": "date"
                },
                "dateIndex": {
                    "type": "date"
                },
                "total": {
                    "type": "long"
                },
                "timestamp": {
                    "type": "long"
                },
                "timestampStr": {
                    "type": "keyword"
                }
            }
        }
    }'

    # Send create index request
    create_response=$(curl -s -o /dev/null -w "%{http_code}" -X PUT "$saveIndexUrl" -H 'Content-Type: application/json' -d "$create_request")

    if [ "$create_response" = "200" ]; then
        echo "Index $saveIndexUrl created successfully."
    else
        echo "Failed to create index $saveIndexUrl, HTTP status code: $create_response"
    fi

fi

# Define a function to encapsulate loop logic
function process_hour() {
    timestamp=$1
    next_timestamp=$((timestamp + $timeSeconds))
    timestamp_ms=$((timestamp * 1000))
    next_timestamp_ms=$((next_timestamp * 1000))
    timestampStr=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($timestamp).strftime('%Y-%m-%d %H:%M:%S'))")
    next_timestampStr=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($next_timestamp).strftime('%Y-%m-%d %H:%M:%S'))")
    id=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($timestamp).strftime('%Y%m%d%H'))")
    dateIndex=$(python3 -c "from datetime import datetime, timezone; print(datetime.fromtimestamp($timestamp, timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'))")
    current_utc_time=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    echo "id: $id"
    echo "timestamp(s): $timestamp"
    echo "timestamp(ms): $timestamp_ms"
    echo "next_timestamp(s): $next_timestamp"
    echo "next_timestamp(ms): $next_timestamp_ms"
    echo "timestampStr: $timestampStr"
    echo "next_timestampStr: $next_timestampStr"
    echo "dateIndex: $dateIndex"
    echo "current_utc_time: $current_utc_time"

    query='{
        "query": {
            "range": {
                "modificationTime": {
                    "gte": "'$timestamp_ms'",
                    "lt": "'$next_timestamp_ms'"
                }
            }
        }
    }'

    # Execute curl command and save the result to a variable
    count_response=$(curl -s -X POST "$userVisitUrl" -H 'Content-Type: application/json' -d "$query")
    total=$(echo "$count_response" | jq -r '.count')

    echo " result $total"

    # Construct the JSON data to be sent
    json_data='{
        "id":"'"$id"'",
        "timestamp":'"$timestamp"'",
        "timestampStr":"'"$timestampStr"'",
        "total": "'"$total"'",
        "dateIndex": "'"$dateIndex"'",
        "createTime": "'"$current_utc_time"'"
    }'

    curl -XPOST "$saveIndexUrl/_doc/$id" -H 'Content-Type: application/json' -d "$json_data"
}

# Get the current time in yyyy-MM-dd HH:mm:ss format
current_time=$(date +"%Y-%m-%d %H:%M:%S")
echo "Current time: $current_time"

begin_timestamp=$(python3 -c "from datetime import datetime, timezone; print(int(datetime.now(timezone.utc).replace(minute=0, second=0, microsecond=0).timestamp()))")
begin_local_time=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($begin_timestamp).strftime('%Y-%m-%d %H'))")

echo "begin_timestamp: $begin_timestamp"
echo "begin_local_time: $begin_local_time"

# Loop by hour - for the last n hours
for ((i=1; i<=$number; i++)); do
    timestamp=$((begin_timestamp - $timeSeconds*i))
    process_hour $timestamp
done