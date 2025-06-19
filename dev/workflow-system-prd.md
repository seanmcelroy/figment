# Figment Workflow System - Product Requirements Document

## Executive Summary

This document outlines the requirements for implementing a workflow system within Figment, accessible through the `jot` console application. The workflow system will provide Windows Workflow Foundation-like capabilities while maintaining Figment's schema-driven, lightweight architecture and avoiding enterprise-specific features like XAML definitions.

## Current State Analysis

### Existing Infrastructure
- **Task System**: Robust task management with filtering, status tracking, and note attachments
- **Schema System**: JSON Schema-based entity definitions with validation
- **Storage Abstraction**: Provider-based storage supporting local files and in-memory persistence
- **Console Interface**: Rich CLI with interactive REPL mode using Spectre.Console
- **Command Architecture**: Hierarchical command system with settings classes and error handling

### Integration Points
- Task entities with status, priority, and assignee tracking
- Pomodoro timer integration for time-based workflow activities
- Project/Context tagging system (@context, +project)
- Ultralist.io-style filtering and querying

## Product Vision

Create a flexible, code-first workflow system that enables users to define, execute, and monitor multi-step processes through the `jot` console application. Workflows should feel natural within the existing task ecosystem while providing powerful automation and orchestration capabilities.

## Core Requirements

### 1. Workflow Definition

#### 1.1 Schema-Based Workflow Definition
- **Workflow Schema**: Define workflows as Figment schemas with standardized fields
- **Activity Schema**: Define individual workflow activities/steps as schemas
- **Transition Schema**: Define rules for moving between activities

#### 1.2 Workflow Properties
```json
{
  "id": "workflow-unique-identifier",
  "name": "Human-readable workflow name",
  "description": "Workflow description",
  "version": "1.0.0",
  "status": "draft|active|deprecated",
  "created": "ISO 8601 timestamp",
  "lastModified": "ISO 8601 timestamp",
  "activities": ["array of activity references"],
  "transitions": ["array of transition rules"],
  "variables": ["workflow-scoped variables"],
  "triggers": ["workflow initiation triggers"]
}
```

#### 1.3 Activity Types
- **Task Activity**: Execute or create tasks within the existing task system
- **Delay Activity**: Wait for specified time duration
- **Decision Activity**: Branch based on conditions or user input
- **Parallel Activity**: Execute multiple activities concurrently
- **Sequential Activity**: Execute activities in sequence
- **Custom Activity**: User-defined activities with configurable properties

### 2. Workflow Execution Engine

#### 2.1 Execution Model
- **Instance-Based**: Each workflow execution creates a workflow instance
- **State Persistence**: Save/restore workflow state across application restarts
- **Concurrent Execution**: Support multiple workflow instances simultaneously
- **Error Handling**: Graceful handling of activity failures with retry policies

#### 2.2 Workflow Instance Properties
```json
{
  "instanceId": "unique-instance-identifier",
  "workflowId": "reference-to-workflow-definition",
  "status": "pending|running|completed|failed|cancelled|suspended",
  "startTime": "ISO 8601 timestamp",
  "endTime": "ISO 8601 timestamp",
  "currentActivity": "reference-to-current-activity",
  "variables": ["instance-specific variable values"],
  "executionHistory": ["array of activity execution records"],
  "errorLog": ["array of error records"]
}
```

#### 2.3 State Management
- Persist workflow instances using existing storage providers
- Support workflow suspension and resumption
- Maintain execution history for debugging and audit trails
- Variable scoping (workflow-level, activity-level)

### 3. Console Interface Integration

#### 3.1 Command Structure
```
jot workflow
├── list                    # List workflow definitions
├── show <workflow-id>      # Show workflow details
├── create <name>           # Create new workflow
├── edit <workflow-id>      # Edit workflow definition
├── delete <workflow-id>    # Delete workflow
├── run <workflow-id>       # Start workflow instance
├── instances              # List workflow instances
├── instance <instance-id> # Show instance details
├── pause <instance-id>    # Pause workflow instance
├── resume <instance-id>   # Resume workflow instance
├── cancel <instance-id>   # Cancel workflow instance
└── history <instance-id>  # Show execution history
```

#### 3.2 Interactive Features
- Rich console output with progress indicators
- Interactive workflow creation wizard
- Real-time workflow execution monitoring
- Integration with existing REPL entity selection system

### 4. Activity System

#### 4.1 Built-in Activities

**Task Activity**
- Create new tasks or update existing tasks
- Assign tasks to users
- Set due dates, priorities, and status
- Wait for task completion before proceeding

**Delay Activity**
- Fixed duration delays (seconds, minutes, hours, days)
- Scheduled delays (wait until specific date/time)
- Business day awareness (skip weekends/holidays)

**Decision Activity**
- Conditional branching based on variable values
- User prompt for manual decisions
- Task completion status checks
- Custom condition evaluation

**Parallel Activity**
- Execute multiple child activities simultaneously
- Wait for all or subset completion
- Merge results from parallel branches

**Sequential Activity**
- Execute child activities in defined order
- Pass data between activities
- Stop on first failure or continue on errors

#### 4.2 Activity Configuration
- JSON-based activity configuration
- Variable substitution support
- Input/output parameter mapping
- Conditional execution rules

### 5. Integration Requirements

#### 5.1 Task System Integration
- Workflow-generated tasks appear in normal task lists
- Task filtering includes workflow-related tasks
- Task completion can trigger workflow progression
- Workflow tasks inherit project/context tags

#### 5.2 Pomodoro Integration
- Time-based activities can integrate with Pomodoro timers
- Workflow activities can be Pomodoro work sessions
- Track time spent on workflow execution

#### 5.3 Data Integration
- Workflows can read/write Thing properties
- Reference other entities (people, projects, etc.)
- Import/export workflow definitions
- Backup/restore workflow instances

## Technical Requirements

### 6.1 Architecture Patterns
- Follow existing command pattern for workflow commands
- Use schema-first approach for workflow definitions
- Implement provider pattern for workflow storage
- Maintain ambient context pattern for global access

### 6.2 Storage Requirements
- Workflow definitions stored as Things using workflow schema
- Workflow instances stored as separate Things
- Support both local file and in-memory storage
- Efficient querying of workflow instances by status/date

### 6.3 Performance Requirements
- Startup time: Workflow commands should load within 100ms
- Execution overhead: Minimal impact on task system performance
- Memory usage: Efficient storage of workflow state
- Concurrent workflows: Support 10+ simultaneous workflow instances

### 6.4 Error Handling
- Graceful handling of activity failures
- Retry policies for transient failures
- Dead letter queue for failed workflow instances
- Comprehensive error logging and reporting

## User Experience Requirements

### 7.1 Learning Curve
- Intuitive command structure following existing patterns
- Built-in help system with examples
- Progressive disclosure of advanced features
- Documentation with common workflow patterns

### 7.2 Workflow Creation
- Interactive workflow creation wizard
- Template-based workflow creation
- Import/export workflow definitions
- Validation of workflow definitions before execution

### 7.3 Monitoring and Debugging
- Real-time workflow execution status
- Detailed execution history
- Error reporting with context
- Performance metrics (execution time, activity duration)

## Success Metrics

### 8.1 Adoption Metrics
- Number of workflow definitions created
- Number of workflow instances executed
- Active workflow users
- Workflow execution success rate

### 8.2 Performance Metrics
- Average workflow execution time
- System resource usage
- Error rates and types
- User satisfaction with workflow features

### 8.3 Integration Metrics
- Usage of workflow-generated tasks
- Integration with existing jot features
- Data flow between workflows and other entities

## Non-Functional Requirements

### 9.1 Compatibility
- Compatible with existing .NET 9.0 runtime
- Works with all existing storage providers
- Maintains backward compatibility with existing schemas
- Cross-platform support (Windows, macOS, Linux)

### 9.2 Security
- No execution of external code or scripts
- Secure handling of workflow variables
- Access control for workflow operations
- Safe serialization/deserialization of workflow state

### 9.3 Maintainability
- Clear separation of workflow engine from UI
- Extensible activity system for future enhancements
- Comprehensive unit test coverage
- Documentation of architecture decisions

## Implementation Phases

### Phase 1: Core Infrastructure (Weeks 1-3)
- Workflow and activity schema definitions
- Basic workflow execution engine
- Storage provider integration
- Core workflow commands

### Phase 2: Built-in Activities (Weeks 4-6)
- Task activity implementation
- Delay and decision activities
- Basic parallel/sequential activities
- Activity configuration system

### Phase 3: Advanced Features (Weeks 7-9)
- Interactive workflow creation
- Workflow monitoring and debugging
- Error handling and retry policies
- Performance optimization

### Phase 4: Integration and Polish (Weeks 10-12)
- Task system integration
- Pomodoro integration
- Comprehensive testing
- Documentation and examples

## Future Considerations

### Potential Enhancements
- Visual workflow designer (future GUI application)
- Workflow templates and marketplace
- Integration with external systems (APIs, webhooks)
- Workflow scheduling and triggers
- Advanced analytics and reporting
- Workflow versioning and migration tools

### Extensibility Points
- Custom activity plugin system
- Workflow trigger extensions
- Storage provider enhancements
- Monitoring and alerting integrations

## Conclusion

This workflow system will provide powerful automation capabilities while maintaining the simplicity and elegance of the existing Figment architecture. By building on the established patterns and integrating seamlessly with the current task system, users will have a natural progression path from simple task management to sophisticated workflow automation.

The phased implementation approach ensures early value delivery while allowing for iterative refinement based on user feedback and usage patterns.