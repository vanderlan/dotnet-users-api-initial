using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UsersApi.Data;

/// <summary>
/// O SQLite não guarda o fuso horário: ao ler, o <see cref="DateTime"/> volta com Kind "Unspecified".
/// Como a aplicação grava sempre em UTC, a leitura marca o valor como UTC e a API serializa com "Z".
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
