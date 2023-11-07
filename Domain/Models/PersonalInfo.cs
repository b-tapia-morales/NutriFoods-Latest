﻿using Domain.Enum;

namespace Domain.Models;

public sealed class PersonalInfo
{
    public Guid Id { get; set; }

    public string Rut { get; set; } = null!;

    public string Names { get; set; } = null!;

    public string LastNames { get; set; } = null!;

    public Gender BiologicalSex { get; set; } = null!;

    public DateOnly Birthdate { get; set; }

    public Patient IdNavigation { get; set; } = null!;
}
