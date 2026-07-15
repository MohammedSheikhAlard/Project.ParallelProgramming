# ⚡ High-Performance E-Commerce Backend Engine

A high-performance, concurrent e-commerce backend system built as a university capstone project for the **Parallel Programming** course. The system simulates a real-world online store capable of handling thousands of simultaneous requests while maintaining data integrity, thread safety, and optimal resource utilization.

## 🎯 Project Overview

The primary goal of this project is to apply advanced parallel and concurrent programming concepts to solve real-world non-functional challenges in e-commerce systems, including race conditions, resource exhaustion, and transaction consistency under heavy load.

## 🏗️ Architecture

The solution follows a **Clean Architecture** approach, separated into three main layers:

### 1. `GeniusesProMax` (API Layer)
The presentation and application layer responsible for handling HTTP requests and orchestrating business logic.
- **Controllers:** RESTful API endpoints for products, orders, and inventory.
- **DTOs:** Data Transfer Objects for clean request/response contracts.
- **Services:** Core business logic and concurrent operations.
- **Interfaces:** Abstractions for dependency injection and testability.
- **Authorization:** Role-based access control.
- **DependencyInjection:** Centralized DI container configuration.

### 2. `GeniusesProMax.LoadBalancer` (Load Distribution Layer)
Simulates request distribution across multiple server instances to prevent single-point bottlenecks and ensure horizontal scalability.

### 3. `Infrastructure` (Data & Cross-Cutting Layer)
Handles all data persistence, caching, and background processing.
- **Data:** DbContext and database configurations.
- **Entities:** Domain models (Products, Orders, Inventory).
- **Migrations:** EF Core database migrations.
- **Caching:** Distributed caching strategy (In-Memory / Redis-ready).
- **BackgroundJob:** Background workers for batch processing and async tasks.
- **Extensions:** Custom middleware and service extensions.

## ✅ Non-Functional Requirements Implemented

| # | Requirement | Implementation |
|---|-------------|----------------|
| 1 | **Concurrent Access & Data Integrity** | Thread-safe inventory modification using concurrent collections and atomic operations, preventing Race Conditions. |
| 2 | **Resource Management & Capacity Control** | Semaphore-based throttling to limit concurrent operations and prevent resource exhaustion. |
| 3 | **Asynchronous Queues** | Non-blocking task offloading for notifications and invoice generation using `Channel<T>` and background services. |
| 4 | **Batch Processing** | Background jobs that process daily sales reports in optimized chunks for maximum throughput. |
| 5 | **Load Distribution** | Simulated load balancer distributing incoming requests across multiple server instances with configurable strategies. |
| 6 | **Distributed Caching** | Caching layer for high-demand products to reduce direct database queries and improve response times. |
| 7 | **Concurrency Control** | Implementation of both **Optimistic Locking** (row versioning) and **Pessimistic Locking** for sensitive inventory updates. |
| 8 | **Transaction Integrity (ACID)** | Composite transactions (payment + inventory update + order creation) that fully succeed or fully rollback, even under concurrent access. |
| 9 | **Stress Testing** | Automated stress tests using `k6` (JavaScript) simulating **100+ concurrent users** without data loss or system crash. |
| 10 | **Benchmarking & Bottleneck Analysis** | Performance profiling comparing response times before and after optimization, identifying the primary bottleneck. |

## 🛠️ Tech Stack

- **Language:** C#
- **Framework:** ASP.NET Core (.NET 8)
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Architecture:** Clean Architecture (API + Infrastructure + LoadBalancer)
- **Concurrency:** `Task`, `Parallel`, `SemaphoreSlim`, `Channel<T>`, `ConcurrentDictionary`
- **Caching:** IMemoryCache / IDistributedCache
- **Stress Testing:** k6 (JavaScript-based load testing)
- **Tools:** Visual Studio, Postman, Git

## 📡 API Endpoints (Examples)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products (cached) |
| GET | `/api/products/{id}` | Get product details |
| POST | `/api/orders` | Place a new order (ACID transaction) |
| PUT | `/api/inventory/{id}` | Update stock (concurrency-safe) |
| GET | `/api/reports/daily-sales` | Trigger batch report generation |

## 🧪 Stress Testing & Benchmarking

The project includes automated stress test scripts located in the API layer:

- **`stress-test.js`**: Simulates 100+ concurrent users performing simultaneous read/write operations.
- **`bottleneck.js`**: Identifies and profiles the primary performance bottleneck in the system.

### Running Stress Tests
```bash
# Install k6
# Run the stress test
k6 run stress-test.js

# Run the bottleneck analysis
k6 run bottleneck.js
