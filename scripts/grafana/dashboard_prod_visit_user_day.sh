#!/bin/bash

# Do not delete logs
#saveIndexUrl="http://xxx/dashboardtestactiveuserdayhistoryindex"
# Mainnet
saveIndexUrl="http://xxx/dashboardproddayuservisitindex"

# Mainnet
userVisitUrl="http://xxx/tmrwdao/userVisit-yesterday"

number=1
# 1 hour
timeSeconds=$((3600 * 24))

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
        echo "Index $saveIndexUrl creation failed, HTTP status code: $create_response"
    fi

fi

# Define a function to encapsulate the loop logic
function process_hour() {
    timestamp=$1
    next_timestamp=$((timestamp + $timeSeconds))
    timestampStr=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($timestamp).strftime('%Y-%m-%d %H:%M:%S'))")
    next_timestampStr=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($next_timestamp).strftime('%Y-%m-%d %H:%M:%S'))")
    id=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($timestamp).strftime('%Y%m%d'))")
    dateIndex=$(python3 -c "from datetime import datetime, timezone; print(datetime.fromtimestamp($timestamp, timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'))")
    current_utc_time=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    echo "id: $id"
    echo "timestampStr: $timestampStr"
    echo "timestamp(s): $timestamp"
    echo "dateIndex: $dateIndex"
    echo "next_hour_timestamp(s): $next_timestamp"  # Note: Corrected variable name from 'next_hour_timestamp' to 'next_timestamp' for consistency
    echo "current_utc_time: $current_utc_time"

    # Execute curl command and save the result to a variable
    response=$(curl "$userVisitUrl")

    echo " result $response"
    # Extract the value of total
    total=$(echo "$response" | jq -r '.[0].fieldValues.activeUsers')

    # Construct the JSON data to be sent
    json_data='{
        "id":"'"$id"'",
        "timestamp":'"$timestamp"'",
        "timestampStr":"'"$timestampStr"'",
        "total": '"$total"'",
        "dateIndex": "'"$dateIndex"'",
        "createTime": "'"$current_utc_time"'"
        }'

    curl -XPOST "$saveIndexUrl/_doc/$id" -H 'Content-Type: application/json' -d "$json_data"

}

# Get the current time in yyyy-MM-dd HH:mm:ss format
current_time=$(date +"%Y-%m-%d %H:%M:%S")
echo "Current time: $current_time"

begin_timestamp=$(python3 -c "from datetime import datetime, timezone; print(int(datetime.now(timezone.utc).replace(hour=0, minute=0, second=0, microsecond=0).timestamp()))")
begin_local_time=$(python3 -c "from datetime import datetime; print(datetime.fromtimestamp($begin_timestamp).strftime('%Y-%m-%d'))")

echo "begin_timestamp: $begin_timestamp"
echo "begin_local_time: $begin_local_time"

# Loop by hour for the last n hours
for ((i=0; i<=$number; i++)); do
    timestamp=$((begin_timestamp - $timeSeconds*i))
    process_hour $timestamp
done