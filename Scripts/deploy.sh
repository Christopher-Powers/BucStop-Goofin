#!/bin/bash
export ASPNETCORE_ENVIRONMENT=Production
# ─────────────────────────────────────────────────────────────
#  Config / Constants
# ─────────────────────────────────────────────────────────────

# Prod IP, still figuring out how we want to manage dev IP not being static
PUBLIC_IP="3.232.16.65"


# Required CLI tools
REQUIRED_CMDS=("git" "docker" "docker-compose" "dotnet")

# List of services for uncontainerized mode
SERVICES=(
    "Team-3-BucStop_APIGateway/APIGateway|API Gateway|8081"
    "Team-3-BucStop_Snake/Snake|Snake|8082"
    "Team-3-BucStop_Pong/Pong|Pong|8083"
    "Team-3-BucStop_Tetris/Tetris|Tetris|8084"
    "Bucstop WebApp/BucStop|BucStop WebApp|8080"
)

# ─────────────────────────────────────────────────────────────
#  Check for required tools
# ─────────────────────────────────────────────────────────────
check_requirements() {
    for cmd in "${REQUIRED_CMDS[@]}"; do
        if ! command -v $cmd >/dev/null 2>&1; then
            echo "❌  Missing required command: $cmd"
            exit 1
        fi
    done
}
check_requirements

# ─────────────────────────────────────────────────────────────
#  Select Deployment Mode (Menu)
# ─────────────────────────────────────────────────────────────
while true; do
    echo -e "\n📌  Select Deployment Mode:"
    echo -e "   [1] 🐳 Containerized (Docker)"
    echo -e "   [2] 🔨 Uncontainerized (Local dotnet run)"
    echo -n "👉  Enter choice (1 or 2): "
    read -r choice

    case $choice in
        1) DEPLOY_MODE="containerized"; break;;
        2) DEPLOY_MODE="uncontainerized"; break;;
        *) echo "❌  Invalid choice! Please enter 1 or 2.";;
    esac
done

# Array to store background process PIDs
declare -a SERVICE_PIDS

# ─────────────────────────────────────────────────────────────
#  Cleanup function
# Stops Docker containers or local processes based on deployment mode.
# Always prunes Docker resources in containerized mode to save EBS space.
# EBS space costs money in AWS.
# ─────────────────────────────────────────────────────────────
cleanup() {
    echo -e "\n🚨  Cleaning up processes..."

    if [[ "$DEPLOY_MODE" == "containerized" ]]; then
        if [ -n "$BUILD_PID" ]; then
            kill "$BUILD_PID" 2>/dev/null
        fi

        echo -e "\n🧹  Stopping Docker containers..."
        docker-compose down

        echo -e "\n🫼  Pruning unused Docker resources..."
        docker system prune -af --volumes | awk '
            /Deleted Images:/ { skip=1; next }
            /Deleted build cache objects:/ { skip=1; next }
            /^Total reclaimed space:/ {
                skip=0
                print "   🧽 " $0
                next
            }
            skip==0 { print }
        '

    else
        echo -e "\n🛌  Stopping local dotnet services..."

        PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

        pgrep -f "dotnet run" | while read -r pid; do
            proc_dir=$(readlink -f /proc/$pid/cwd 2>/dev/null)

            if [[ "$proc_dir" == "$PROJECT_ROOT"* ]]; then
                echo "Stopping dotnet process $pid from $proc_dir..."
                kill "$pid" 2>/dev/null
            fi
        done
    fi

}

# Bind cleanup to Ctrl+C and termination signals
trap cleanup SIGINT SIGTERM

# ─────────────────────────────────────────────────────────────
#  Timer display during builds for feedback
# ─────────────────────────────────────────────────────────────
timer() {
    local start_time=$(date +%s)
    local pid=$1

    echo -n "⏳  Building services... Elapsed time: 00:00"

    while kill -0 "$pid" 2>/dev/null; do
        sleep 1
        local current_time=$(date +%s)
        local elapsed=$((current_time - start_time))
        local minutes=$((elapsed / 60))
        local seconds=$((elapsed % 60))
        printf "\r⏳  Building services... Elapsed time: %02d:%02d" "$minutes" "$seconds"
    done

    echo -e "\r✅  Services built in $minutes minutes and $seconds seconds.    "
}

# ─────────────────────────────────────────────────────────────
#  Function to build and run services without containers
# ─────────────────────────────────────────────────────────────
build_uncontainerized() {
    echo -e "\n🔨  Building services locally..."

    PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

    build_and_run_service() {
        local service_path=$1
        local service_name=$2
        local port=$3

        echo -e "\n📦  Building $service_name..."
        cd "$PROJECT_ROOT/$service_path" || {
            echo "❌  Failed to change to directory: $service_path"
            return 1
        }

        if ! dotnet build; then
            echo "❌  Failed to build $service_name"
            return 1
        fi

        echo -e "🚀  Starting $service_name on port $port..."
        ASPNETCORE_URLS="http://0.0.0.0:$port" \
        # There is currently only a "development" environment variable outlined in launchSettings.json
        # and no Prod environment variable.
        ASPNETCORE_ENVIRONMENT="Development" \
        dotnet run --no-launch-profile &
        local pid=$!
        SERVICE_PIDS+=($pid)

        sleep 2
        if ! kill -0 $pid 2>/dev/null; then
            echo "❌  Failed to start $service_name"
            return 1
        fi

        echo "✅  $service_name started successfully (PID: $pid)"
        cd - > /dev/null || exit 1
    }

    for service in "${SERVICES[@]}"; do
        IFS="|" read -r path name port <<< "$service"
        if ! build_and_run_service "$path" "$name" "$port"; then
            echo "❌  Deployment failed. Cleaning up..."
            cleanup
        fi
    done

    echo -e "\n✅  All services built and started successfully!"
    echo -e "📜  Services are running on:"
    for service in "${SERVICES[@]}"; do
        IFS="|" read -r path name port <<< "$service"
        echo -e "   - $name: http://$PUBLIC_IP:$port"
    done
    echo
}

# ─────────────────────────────────────────────────────────────
#  Deployment Magic Starts Here
# ─────────────────────────────────────────────────────────────
echo "🚀  Starting deployment process..."

# Pull latest repo updates
echo "🔄  Checking repository status..."
pull_output=$(git pull)
if [[ "$pull_output" == "Already up to date." ]]; then
    echo "✅  Repo is already up to date."
else
    echo "$pull_output"
fi

if [[ "$DEPLOY_MODE" == "containerized" ]]; then
    # Clean up Docker resources
    cleanup

    echo -e "\n🐳  Launching microservices..."
    (docker-compose up -d > /dev/null 2>&1) &
    BUILD_PID=$!
    timer $BUILD_PID
    echo -e "✅  All containerized services started successfully!\n"
else
    build_uncontainerized
fi


# Notes: 
# I am not 100% certain I am in love with the cleanup mechanism.
# Consider revisiting cleanup for containers both before and after deployment actions.
# Consider using 'killall dotnet' as blunt force to kill dotnet services instead.
# Consider altering `docker system prune` based on our approach to data persistence.
