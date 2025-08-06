# AI Memory Management System

**Purpose**: Systematic approach to information persistence and retrieval across different temporal contexts.

## Memory Hierarchy

### 🧠 Long-Term Memory (Permanent)
**Location**: Core system files  
**Purpose**: Persistent knowledge that should never be lost  
**Retention**: Indefinite

**Components**:
- **`copilot-instructions.md`**: Behavioral patterns, proven methodologies, workflow optimization
- **`changelog.md`**: Historical achievements, major decisions, lessons learned
- **`backlog.md`**: Strategic priorities, architectural roadmap, future planning
- **`docs/`**: Architecture decisions, partnership evolution, technical references
- **`projects/archive/`**: Completed project plans with full context and outcomes

**Access Pattern**: Reference frequently for decision-making and pattern recognition

### 🎯 Short-Term Memory (Context)
**Location**: Active project directory  
**Purpose**: Current work context and immediate planning  
**Retention**: Until project completion and archival

**Components**:
- **`projects/active/`**: Current project plans, requirements, acceptance criteria
- **`projects/templates/`**: Reusable structures and patterns
- **Session conversation**: Current discussion context and immediate decisions

**Access Pattern**: Primary reference for ongoing work and immediate next steps

### ⚡ Working Memory (Session)
**Location**: Temporary files and logs  
**Purpose**: Detailed implementation tracking and immediate problem-solving  
**Retention**: Temporary - summarize and delete after use

**Components**:
- **`logs/`**: Step-by-step implementation details (temporary)
- **`test-scripts/`**: Validation scripts (keep core, remove debug)
- **`_scratch/`**: Experimental analysis and temporary notes
- **In-session context**: Current conversation state and immediate decisions

**Access Pattern**: Heavy use during active work, minimal retention after completion

## Memory Management Protocol

### Information Flow
1. **Working → Short-Term**: Summarize session achievements in project plans
2. **Short-Term → Long-Term**: Archive completed projects, update instructions with learnings
3. **Long-Term → All**: Reference patterns and decisions in current work

### Cleanup Rules
- **Working Memory**: Delete detailed logs after summarizing in changelog
- **Short-Term Memory**: Archive complete projects immediately after completion
- **Long-Term Memory**: Only add proven patterns and significant learnings

### Access Priorities
1. **Start session**: Check active projects (short-term) and instructions (long-term)
2. **During work**: Reference relevant docs and patterns from long-term memory
3. **Problem-solving**: Create working memory artifacts, reference all levels
4. **End session**: Summarize working memory, update short/long-term as needed

## Memory Retrieval Strategies

### For Project Planning
- **Long-term**: Review similar archived projects, proven patterns
- **Short-term**: Check current active scope and dependencies
- **Working**: Create session logs for complex implementation

### For Decision Making
- **Long-term**: Reference copilot instructions, architecture docs
- **Short-term**: Consider current project constraints and objectives
- **Working**: Document decision rationale and implementation steps

### For Problem Solving
- **Long-term**: Look for similar issues in changelog and archived projects
- **Short-term**: Check if problem affects current project scope
- **Working**: Create temporary analysis files, document solutions

## Implementation Notes

This memory system enables:
- **Pattern Recognition**: Long-term memory provides proven approaches
- **Context Continuity**: Short-term memory maintains project focus
- **Implementation Tracking**: Working memory captures detailed progress
- **Knowledge Transfer**: Systematic flow between memory levels preserves learnings

The goal is to create an **external memory system** that enables autonomous decision-making and learning persistence across sessions and projects.
