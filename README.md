# 🏋️ Workout Tracker App

A cross-platform fitness tracker built with **.NET MAUI**, designed to help users stay on top of their fitness goals by logging cardio and weightlifting workouts, tracking progress, and viewing performance data.

---

## 🚀 Features

- **User Accounts**
  - Sign up and log in with a simple in-memory auth system.

- **Weightlifting Logging**
  - Track exercises by muscle group, with sets, reps, and weights.

- **Cardio Sessions**
  - Real-time step tracking using Android’s Step Counter and Step Detector sensors.

- **Dashboard**
  - Filter workouts by date to stay organized.

- **Leaderboard**
  - View workouts by gym location to create friendly competition.

- **Workout Library**
  - Search for exercises by name or muscle group.

- **Dynamic Navigation**
  - Flyout menu updates based on login status.

---

## 🧰 Tech Stack

- .NET MAUI (cross-platform UI framework)
- MVVM architecture (clean separation of UI & logic)
- Dependency Injection via `MauiProgram.cs`
- Android Sensor APIs for step tracking

---

## 🛠 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- Visual Studio 2022 or later with the **.NET MAUI** workload
- Android Emulator or real Android device

### Setup

```bash
git clone https://github.com/twoody0/WorkoutTracker.git
cd WorkoutTracker
dotnet build
dotnet run
```

> 📱 For Android: make sure to grant **Activity Recognition** permissions when requested.

---

## 📂 Project Structure

```
├── Models/                  # Workout, User, Exercise definitions
├── Services/               # WorkoutService, AuthService, StepCounterService
├── ViewModels/             # View logic and binding commands
├── Views/                  # All .xaml UI files and code-behinds
├── Platforms/Android/      # Platform-specific step tracking
├── AppShell.xaml           # Flyout menu and routing
├── MauiProgram.cs          # DI and app configuration
```

---

## 📈 What's Next?

- Add SQLite to persist user and workout data
- Secure passwords with hashing (e.g. BCrypt)
- Sync workouts to the cloud with a web API
- Add charting to visualize progress
- Write unit tests for services and view models

---

## 👋 Author

**Tyler Woody**  
Built with .NET MAUI, a lot of coffee, and a love for clean code.

---

## 📃 License

This project is open-source and available under the [MIT License](LICENSE).
