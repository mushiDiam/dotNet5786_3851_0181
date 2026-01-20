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

## ? Features

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

## ?? Technologies

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
```
dotnet run --project DalTest
```

**BL Test Console:**
```
dotnet run --project BlTest
```

### Running the Application
```
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

---

## ?? Bonuses Implemented

| Category | Bonus | Points | Implementation |
|----------|-------|--------|----------------|
| **Development Environment** | Proper TryParse usage with return value validation | 1 | [BlTest/Program.cs](BlTest/Program.cs), [DalTest/Program.cs](DalTest/Program.cs) |
| **DAL** | Password property for Courier entity | 2 | [DalFacade/DO/Courier.cs](DalFacade/DO/Courier.cs) |
| **DAL** | Singleton with Thread Safety + Lazy Initialization | 2 | [DalList/DalList.cs](DalList/DalList%20.cs), [DalXml/DalXml.cs](DalXml/DalXml.cs) |
| **BL** | Initial password set by manager + user update capability | 1 | [PL/Login/ChangePasswordWindow.xaml.cs](PL/Login/ChangePasswordWindow.xaml.cs) |
| **PL - WPF** | Property Triggers (IsMouseOver effects) | 1 | [PL/Login/LoginWindow.xaml](PL/Login/LoginWindow.xaml), [PL/Config/MainWindow.xaml](PL/Config/MainWindow.xaml) |
| **PL - WPF** | Data Triggers | 1 | [PL/Courier/ForManager/CourierListWindow.xaml](PL/Courier/ForManager/CourierListWindow.xaml) |
| **PL - WPF** | Multi Data Triggers | 1 | [PL/Courier/ForManager/CourierListWindow.xaml](PL/Courier/ForManager/CourierListWindow.xaml) |
| **PL - WPF** | ControlTemplate usage | 1 | [PL/Config/MainWindow.xaml](PL/Config/MainWindow.xaml), [PL/Login/LoginWindow.xaml](PL/Login/LoginWindow.xaml) |
| **PL - WPF** | Application Icon | 1 | [PL/Helpers/app.ico](PL/Helpers/app.ico) |
| **PL - Project** | Password hidden with asterisks (PasswordBox) | 1 | [PL/Login/LoginWindow.xaml](PL/Login/LoginWindow.xaml) |
| **PL - Project** | Smart delete button (visible only when deletion is allowed) | 2 | [PL/Courier/ForManager/CourierListWindow.xaml](PL/Courier/ForManager/CourierListWindow.xaml) |
| **PL - Project** | Address error handling with clear reason display | 2 | [PL/Config/MainWindow.xaml.cs](PL/Config/MainWindow.xaml.cs) |
| **Simulator** | Loading indicator (Cursors.Wait) | 1 | [PL/Config/MainWindow.xaml.cs](PL/Config/MainWindow.xaml.cs) |

### ?? Total Bonus Points: 17

---

### Implementation Details

#### Singleton with Lazy Initialization (Thread Safe)
```csharp
// DalList/DalList.cs & DalXml/DalXml.cs
private static readonly Lazy<IDal> _instance = new(() => new DalList());
public static IDal Instance => _instance.Value;
```
This pattern ensures:
- **Lazy**: Instance created only when first accessed
- **Thread Safe**: `Lazy<T>` is thread-safe by default in .NET

#### Smart Delete Button
The delete button in courier list only appears when:
- Courier has no completed deliveries (`OrdersOnTime + OrdersLate == 0`)
- Courier has no active order (`CurrentOrderId == null`)

#### Address Error Handling
When coordinates cannot be resolved from an address, a clear error message is displayed:
```csharp
if (coords.Lat == null || coords.Lon == null)
{
    MessageBox.Show("Cannot find this address on the map.\nPlease check spelling or try a more specific address.",
                    "Invalid Address", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

---

## ?? License

This project was created for educational purposes as part of the JCT (Jerusalem College of Technology) curriculum.

---

Thank you for reviewing our project! ??