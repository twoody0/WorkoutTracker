﻿using System.Diagnostics.CodeAnalysis;

namespace WorkoutTracker.Models;

public enum WorkoutType
{
    WeightLifting,
    Cardio
}

public class Workout
{
    public required string Name { get; set; } = default!;
    public required string MuscleGroup { get; set; } = default!;
    public required string GymLocation { get; set; } = default!;

    public double Weight { get; set; }
    public int Reps { get; set; }
    public int Sets { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Steps { get; set; }
    public WorkoutType Type { get; set; }
    public DayOfWeek Day { get; set; }

    public Workout() { }

    [SetsRequiredMembers]
    public Workout(string name, double weight, int reps, int sets, string muscleGroup, DayOfWeek day, DateTime startTime, WorkoutType type, string gymLocation)
    {
        Name = name;
        Weight = weight;
        Reps = reps;
        Sets = sets;
        MuscleGroup = muscleGroup;
        Day = day;
        StartTime = startTime;
        Type = type;
        GymLocation = gymLocation;
    }

    [SetsRequiredMembers]
    public Workout(
    string name,
    double weight,
    int reps,
    int sets,
    string muscleGroup,
    DateTime startTime,
    WorkoutType type,
    string gymLocation)
    : this(name, weight, reps, sets, muscleGroup, DayOfWeek.Monday, startTime, type, gymLocation)
    {
        // Default DayOfWeek to Monday if not provided
    }
}
