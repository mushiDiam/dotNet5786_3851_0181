# Delivery Management System

A comprehensive delivery management application built with .NET 8, developed as part of the "Mini Project in Windows Systems" course.

**Authors:** Moshe Diamandi & David Uzan

**Repository:** [GitHub](https://github.com/mushiDiam/dotNet5786_3851_0181)

---

## ?? Project Overview

This project is a multi-layered delivery management system that handles orders, couriers, and deliveries. It follows a clean architecture with separate Data Access Layer (DAL), Business Logic Layer (BL), and Presentation Layer (PL).

## ??? Architecture

The solution follows a **3-tier architecture**:

```
???????????????????????????????????????
?      PL (Presentation Layer)        ?
?         WPF Application             ?
???????????????????????????????????????
?      BL (Business Logic Layer)      ?
?    Order, Courier, Admin Services   ?
???????????????????????????????????????
?      DAL (Data Access Layer)        ?
?       DalList / DalXml              ?
???????????????????????????????????????
```

## ?? Project Structure

| Project | Description |
|---------|-------------|
| `DalFacade` | Interfaces and data entities for the DAL |
| `DalList` | In-memory list-based DAL implementation |
| `DalXml` | XML file-based DAL implementation |
| `DalTest` | Console application for testing DAL functionality |
| `BlApi` | Business Logic interfaces |
| `BL` | Business Logic implementation |
| `BlTest` | Console application for testing BL functionality |
| `PL` | WPF Presentation Layer |

## ?? Features

### Order Management
- Create, read, update, and cancel orders
- Track order status (Open, InProgress, Closed, Denied, Cancelled)
- Schedule status monitoring (OnTime, Late, InRisk)
- Support for various order types (Food, Gifts, Health, Supplies, Pets)

### Courier Management
- Courier registration and authentication
- Support for multiple transportation modes (Car, Motorcycle, Bike, Walking)
- Active order tracking per courier
- Delivery performance metrics (on-time vs. late deliveries)

### Admin Features
- System clock management (simulated time for testing)
- Database initialization and reset
- Configuration management
- Simulator for automated delivery processing

## ??? Technologies

- **.NET 8**
- **C# 12**
- **WPF** (Windows Presentation Foundation)
- **XML** for data persistence
- **Factory Pattern** for DAL abstraction
- **Observer Pattern** for UI updates

## ?? Configuration

The system supports configurable settings including:

- Company address and coordinates
- Maximum delivery distance
- Average speeds for each transportation type
- Maximum delivery time constraints
- Risk range thresholds
- Courier inactivity timeout

## ?? Running the Project

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022

### Running the Tests

**DAL Test Console:**
```sh
dotnet run --project DalTest
```

**BL Test Console:**
```sh
dotnet run --project BlTest
```

### Running the Application
```sh
dotnet run --project PL
```

Or open the solution in Visual Studio and set `PL` as the startup project.

## ?? Data Entities

### Order
- Customer information (name, phone)
- Delivery address with coordinates
- Order properties (weight, volume, fragile)
- Timestamps and status tracking

### Courier
- Personal details (name, phone, email)
- Transportation preference
- Maximum delivery distance
- Activity status and performance metrics

### Delivery
- Links order to courier
- Tracks delivery progress
- Records actual distance and completion status

## ?? Authentication

The system supports two roles:
- **Manager** - Full access to all features, configuration, and simulator
- **Courier** - Access to personal deliveries and order management

## ?? License

This project was created for educational purposes as part of the JCT (Jerusalem College of Technology) curriculum.

---

Thank you for reviewing our project! ??