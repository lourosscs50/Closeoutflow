# Closeoutflow

Closeoutflow is a modular closeout workflow platform for tracking jobs, managing closeout proof, and validating project completion through clean domain boundaries and test-driven foundations.

Project domain: **closeoutflow.org**

The project is currently focused on building a reliable backend foundation before adding UI, persistence, authentication, or external integrations.

## Why Closeoutflow Exists

Many field-service, construction, maintenance, and operations teams complete the physical work before the administrative closeout is fully verified. That gap can create delays, missing proof, unclear job status, and weak audit trails.

Closeoutflow is designed to model that workflow clearly:

- A job is created with operator-provided input.
- A job moves through controlled status transitions.
- A job becomes eligible for closeout only after reaching the correct state.
- A closeout record requires proof.
- A completed closeout closes the job.
- Invalid closeout attempts fail safely without corrupting job state.
- Operators can read jobs, closeouts, and closeouts attached to a specific job.

## Current Capabilities

The current backend foundation supports:

- Creating jobs with a requested title.
- Listing jobs.
- Reading jobs by ID.
- Starting jobs.
- Marking jobs as pending closeout.
- Completing job closeouts with proof items.
- Listing closeout records.
- Reading closeout records by ID.
- Listing closeout records for a specific job.
- Validating required closeout proof.
- Preventing invalid job state transitions.
- Preserving job state when closeout validation fails.
- Explicit API response contracts.
- In-memory repositories for early local development.
- Module boundary tests to protect architecture.

## Architecture

Closeoutflow is built as a modular .NET solution with clear boundaries.

```text
src/
  Closeoutflow.Api
  Closeoutflow.Modules.Jobs
  Closeoutflow.Modules.Closeouts
  Closeoutflow.Shared

tests/
  Closeoutflow.Api.Tests
  Closeoutflow.Modules.Jobs.Tests
  Closeoutflow.Modules.Closeouts.Tests
  Closeoutflow.Architecture.Tests
