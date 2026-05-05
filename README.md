# Industrial Processing System

Kolokvijum 1 — thread-safe servis za obradu poslova (producer/consumer sa
prioritetima, asinhrono izvrsavanje, retry, dogadjaji, periodicni izvestaj).

## Pokretanje

```bash
dotnet build
dotnet run --project src/IPS.App
```

`SystemConfig.xml` se kopira u output dir automatski.

Sa Ctrl+F5 (bez debugger-a) jer se pri timeout-u baca `OperationCanceledException`
po dizajnu.

## Testovi

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Rezultat poslednjeg pokretanja: `Coverage.txt` (sazetak) i
`coverage.cobertura.xml` (puni izvestaj).

## Struktura

```
src/
  IPS.Core/         biblioteka — Job, JobHandle, ProcessingSystem, logger, izvestaj
    Processing/     PrimeJob, IoJob, PayloadParser
  IPS.App/          konzolna aplikacija sa producer-ima
tests/
  IPS.Tests/        xUnit testovi
```
