# Travel Planner Architecture

## Multi-Agent Workflow Overview

This application demonstrates a **code-generated multi-agent system** where specialized AI agents collaborate to create comprehensive travel plans. The workflow orchestrates 6 specialized agents across 4 phases:

```mermaid
graph LR
    subgraph "Phase 1: Parallel Gathering"
        Currency[Currency Agent]
        Weather[Weather Agent]
        Local[Local Knowledge Agent]
    end
    
    subgraph "Phase 2: Itinerary"
        Itinerary[Itinerary Planner Agent]
    end
    
    subgraph "Phase 3: Budget"
        Budget[Budget Optimizer Agent]
    end
    
    subgraph "Phase 4: Assembly"
        Coordinator[Coordinator Agent]
    end
    
    Currency --> Itinerary
    Weather --> Itinerary
    Local --> Itinerary
    Itinerary --> Budget
    Budget --> Coordinator
```

### Specialized Agents

1. **Currency Converter Agent** - Real-time exchange rates (Frankfurter API)
2. **Weather Advisor Agent** - Weather forecasts and packing tips (NWS API)
3. **Local Knowledge Agent** - Cultural insights and local customs
4. **Itinerary Planner Agent** - Day-by-day activity scheduling
5. **Budget Optimizer Agent** - Cost allocation and optimization
6. **Coordinator Agent** - Final assembly and formatting

### Workflow Execution

**Phase 1 (10-40%)**: Parallel information gathering
- Currency, Weather, and Local agents run simultaneously
- External API calls for real-time data
- Results stored in workflow state

**Phase 2 (40-70%)**: Itinerary creation
- Uses context from Phase 1 (weather, local knowledge)
- Creates detailed daily activities and dining

**Phase 3 (70-90%)**: Budget optimization
- Analyzes itinerary costs
- Allocates budget across categories

**Phase 4 (90-100%)**: Final assembly
- Coordinator compiles all agent outputs
- Formats comprehensive travel plan

## High-Level System Overview

```mermaid
flowchart TB
    User[User]
    UI[Web UI - Static HTML]
    API[App Service API - .NET 9.0]
    WebJob[Continuous WebJob - Background Worker]
    ServiceBus[Service Bus - Async Queue]
    Cosmos[Cosmos DB - Task Status & Results]
    AI[Azure AI Foundry - GPT-4o + Agent Framework]

    User -->|1. Submit Request| UI
    UI -->|2. POST /api/travel-plans| API
    API -->|3. Queue Message| ServiceBus
    API -->|4. Store Status| Cosmos
    API -->|5. Return TaskId| UI
    UI -->|6. Poll Status| API
    ServiceBus -->|7. Process Message| WebJob
    WebJob -->|8. Generate Plan| AI
    AI -->|9. Return Itinerary| WebJob
    WebJob -->|10. Save Result| Cosmos
    Cosmos -->|11. Return Complete| UI
    UI -->|12. Display| User

    style User fill:#e1f5ff
    style UI fill:#e1f5ff
    style API fill:#fff4e1
    style WebJob fill:#ffd4a3
    style ServiceBus fill:#ffe1f5
    style Cosmos fill:#e1ffe1
    style AI fill:#f5e1ff
```

## How It Works

### 1. **User Submits Travel Request**
User fills out form with destination, dates, budget, interests, and preferences.

### 2. **API Creates Async Task**
- API creates task in Cosmos DB with status "queued"
- Sends message to Service Bus queue
- Returns taskId immediately (non-blocking)

### 3. **Background Processing**
- Continuous WebJob picks up message from queue
- Updates task status to "processing"
- Calls Azure AI Foundry to generate itinerary

### 4. **AI Multi-Agent Generation**
- WebJob executes TravelPlanningWorkflow orchestrator
- Workflow creates and manages 6 specialized agents
- **Phase 1**: Currency, Weather, Local Knowledge agents run in parallel
- **Phase 2**: Itinerary Planner agent creates daily schedule
- **Phase 3**: Budget Optimizer agent allocates funds
- **Phase 4**: Coordinator agent assembles final plan
- Each agent is a persistent AI agent created on Azure AI Foundry
- Agents use GPT-4o with specialized instructions
- External APIs provide real-time data (weather, currency)

### 5. **Store and Return Results**
- Parses AI response and extracts travel tips
- Updates Cosmos DB with completed itinerary
- Task status changes to "completed"

### 6. **UI Polling and Display**
- UI polls API every second for status updates
- Shows progress bar during processing
- Displays formatted itinerary when complete

## Key Architecture Patterns

### ✅ Async Request-Reply Pattern
- API returns immediately with taskId
- Client polls for status updates
- No long-running HTTP connections

### ✅ Background Processing with WebJobs
- Continuous WebJob runs as separate process on App Service
- Service Bus decouples API from heavy AI work
- Enables independent restarts and monitoring
- Retry logic for reliability

### ✅ Azure AI Agent Framework
- **Multi-Agent Workflows**: Code-defined orchestration of specialized agents
- **Parallel Execution**: Independent agents run simultaneously
- **Server-side persistent agents**: Agents created on Azure AI Foundry infrastructure
- **Managed lifecycle**: Agents created per workflow phase, cleaned up after use
- **External API Integration**: Weather and currency APIs enhance agent capabilities
- **Conversation context**: Each agent maintains thread-based conversation history

### ✅ Managed Identity
- No credentials in code
- Secure authentication to all Azure services

### ✅ State Management
- Cosmos DB stores all task state
- 24-hour TTL for automatic cleanup

## Key Features

### WebJob Architecture
- **Separate Process**: WebJob runs independently from the API
- **Independent Restart**: Restart WebJob without affecting API
- **Dedicated Logging**: WebJob logs separate from API logs in Azure Portal
- **Single Instance**: WEBJOBS_RUN_ONCE prevents duplicate message processing
- **Continuous Execution**: Always-on WebJob for immediate message processing

### Asynchronous Processing
- **Request-Reply Pattern**: Client submits request → receives taskId → polls for status
- **Background Processing**: Service Bus ensures reliable async execution
- **Progress Tracking**: Real-time status updates (queued → processing → completed)

### Azure AI Agent Framework
- **Persistent Agents**: Server-side agents with threads and runs
- **Managed Lifecycle**: Agents created per request and cleaned up after use
- **Conversation Context**: Thread-based conversation management
- **AI Orchestration**: GPT-4o model with custom instructions

### State Management
- **Cosmos DB**: Centralized state storage with TTL for auto-cleanup
- **Task Status**: Tracks progress and stores results
- **24-Hour TTL**: Automatic cleanup of old travel plans

### Scalability
- **Premium App Service**: P0v4 Windows tier for production workloads
- **Service Bus**: Decouples API from processing for horizontal scaling
- **Managed Identity**: Secure, credential-less authentication
- **WebJob Worker**: Dedicated process for heavy AI processing

### Reliability
- **Retry Logic**: Service Bus max 3 delivery attempts
- **Dead Letter Queue**: Failed messages moved to DLQ after max retries
- **Error Handling**: Comprehensive try-catch with logging
- **Status Tracking**: Detailed progress and error messages
- **Duplicate Prevention**: WebJob checks if task already completed before reprocessing
- **Cleanup**: Automatic agent deletion and 24-hour document expiration
