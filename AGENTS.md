# AI Agent Instructions for LoadHitchZa

## Issue Tracking System

**IMPORTANT**: This project uses `bd` (Beads) for task tracking instead of markdown TODO lists.

### Before Starting Each Session

1. **Check ready work**: `bd ready --json` to see unblocked tasks
2. **Review context**: `bd show <issue-id> --json` for task details
3. **Check dependencies**: `bd dep tree <issue-id>` to understand blockers

### During Work

1. **Claim tasks**: `bd update <issue-id> --status in_progress --json`
2. **Create discovered issues**: `bd create "Found bug" --description="Details" -p 1 -t bug --deps discovered-from:<parent-id> --json`
3. **Update progress**: Add notes with `bd update <issue-id> --notes "Progress update"`

### Before Ending Session

1. **Update issue status**: Close completed tasks with `bd close <issue-id> --reason "Completed" --json`
2. **Create follow-up issues**: File issues for remaining work
3. **Sync database**: `bd sync` to ensure all changes are committed

### Issue Types

- `feature`: New functionality
- `bug`: Something broken that needs fixing
- `task`: Work items (tests, docs, refactoring)
- `epic`: Large features with multiple subtasks
- `chore`: Maintenance (dependencies, tooling)

### Priority Levels

- `0`: Critical (security, data loss, broken builds)
- `1`: High (major features, important bugs)
- `2`: Medium (nice-to-have features, minor bugs) - DEFAULT
- `3`: Low (polish, optimization)
- `4`: Backlog (future ideas)

## Project Context

### Technology Stack
- ASP.NET Core 8.0 (C#)
- Blazor Server
- Entity Framework Core
- PostgreSQL database
- PayFast payment gateway
- Mapbox Directions API
- Azure deployment

### Key Services
- **PricingService**: Dynamic pricing with surge pricing, 8 load categories, Mapbox integration
- **PaymentService**: Escrow-based payment lifecycle (Pending → Held → Released/Refunded)
- **PayFastService**: Payment gateway integration with webhook validation
- **NotificationService**: Email/SMS notifications for customers and drivers

### Database Models
- **Load**: Enhanced with pricing fields (WeightKg, LoadCategory, DistanceKm, pricing breakdown)
- **Payment**: Escrow transactions with 15% platform fee
- **PricingTier**: Configurable pricing by category and weight range
- **DriverLocation**: Real-time GPS tracking

## Current Sprint: PayFast Integration Complete

### Completed Work
✅ Weight conversion (pounds → kilograms)
✅ Dynamic pricing service with Mapbox
✅ Escrow payment service
✅ PayFast payment gateway integration
✅ PayFast webhook controller
✅ Payment form Razor component
✅ Sandbox configuration
✅ Comprehensive documentation

### Next Priorities
See `bd ready` for current task queue ordered by priority.

## Notes for AI Agents

- Always use `--json` flag for programmatic operations
- Link discovered work with `--deps discovered-from:<parent-id>`
- Update issue descriptions as you learn more about the work
- Use `bd dep add <issue-id> <blocker-id> --type blocks` to mark dependencies
- Check `bd stats` to see overall progress
