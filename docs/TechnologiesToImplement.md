Event Sourcing:
- Saves events instead of state, Marten is a good .NET library that supports it with PostgreSQL.
- Benefits: Audit trail, time travel, debugging
- Challenges: Complexity, eventual consistency, needs snapshots for performance
- Events are immutable records of something that happened.
	'''public record AccountOpened(Guid AccountId, DateTime OpenedAt);'''
- Records in .NET are a perfect fit for events, as they are immutable by design.
- Events are persisted in an append-only log, never modfied or deleted.

Keep an eye on asynchronous programming, improving scalability and responsiveness.

[https://www.milanjovanovic.tech/blog/6-steps-for-setting-up-a-new-dotnet-project-the-right-way]
Code Style
- .editorconfig from .NET runtime repo: https://github.com/dotnet/runtime/blob/main/.editorconfig
- Jovanovic's .editorconfig example: https://gist.github.com/m-jovanovic/417b7d0a641d7dd7d1972550fba298db

Central build configuration
- add Directory.Build.props file in solution root
- remove build configuration from .csproj files

Central package management
- add Directory.Build.targets file in solution root
- remove package versions from .csproj files

Static code analysis

Local Orchestration
- docker compose
- .net aspire

Continuous integration and deployment
- github actions
- https://www.milanjovanovic.tech/blog/how-to-build-ci-cd-pipeline-with-github-actions-and-dotnet

Architecture testing:
- https://www.milanjovanovic.tech/blog/shift-left-with-architecture-testing-in-dotnet

Testcontainers integration testing:
- https://www.milanjovanovic.tech/blog/testcontainers-integration-testing-using-docker-in-dotnet

Raw SQL:
- for complex queries that do not translate well with ORMs
- for database specific features full-text search, json operators, common table expressions
- for atomic operations with proper locking
- for reducing round trips for example aggregations from multiple tables

Stored function:
- designed to return values

Stored procedure:
- sql command FOR UPDATE locks the row
- sql command IF validates business rules
- sql command EXCEPTION provides clear error message
- keeps everything atomic in single round trip
- can be done in C# with manual transaction management and explicit locking, but it's more complex and error prone

Domain: Entities, Rules,
Application: Orchestration of use cases (handler coordinates loading, transformation, validation, saving without business logic),
Infrastructure: EF Core implementations, repository pattern, 3rd party integrations
API: Controllers as tiny wrappers
- Expectation: business logic testable in isolation without infrastructure concerns or HTTP server

Dependency Injection: Clear separation of concerns thanks to everything depending on abstractions (interfaces) rather than concrete implementations.

Singleton: one instance for the entire application lifetime, stateless services, (DbContext not thread-safe thus cross-thread exceptions)
Scoped: one instance per web request, (DbContext approved)
Transient: new instance every injection

Commands: commands went through domain entities - CreateDeviceCommandHandler worked with Device entity, enforced business rules, and saved changes.
- writes would go to master database.
Queries: queries completely separated from commands - use EF with AsNoTracking projecting to DTO. Make sure to retrieve just what is needed.
- possible use of materialized views or read replicas.

Deferred execution: build IQueryable request and run it only once by calling await query.ToListAsync() or iterate in foreach.

Eager loading: use .Include() to load related entities in single query to avoid N+1 problem.

Change Tracking: EF Core creates snapshot of entity when loaded, compares on SaveChanges to generate SQL UPDATEs.
- .AsNoTracking() removes snapshots for read-only queries, saving memory and reducing query time.

Transaction: logical unit of work, all operations succeed or fail together. (SaveChanges, Publish events, log audit record)
- Transaction begins, N operations performed, if all succeed, commit transaction, else rollback to state before transaction began. No partial changes.

Atomicity: all operations within a transaction are indivisible, either all succeed or none do.
Consistency: transaction brings database from one valid state to another, maintaining integrity constraints.
Isolation: concurrent transactions do not interfere with each other, intermediate states are not visible.
Durability: once a transaction is committed, changes are permanent even in case of system failure.

Database Indexing: - B-tree indexes for equality and range queries, similiar to book index. Milions vs tens of reads.
- Challenges: requires additional storage, slows down writes, needs maintenance, adding row may result in updating multiple indexes.

Connection pool(s): monitor active connections, max pool size, timeouts, errors.

Key implementations for Enterprise Solutions:
Immutability - Value objects, private setters, readonly collections
Encapsulation - Business logic in domain, not controllers
Factory Methods - Control object creation
Value Objects - Type safety, validation at construction
Domain Events - Decouple aggregates
CQRS - Separate read/write models
Result Pattern - No exceptions for business logic failures
Records - DTOs, commands, queries
Required Properties - Compiler-enforced initialization
Backing Fields - Control collection modification
