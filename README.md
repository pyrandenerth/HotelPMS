# HotelPMS

Сетевое программное средство управления номерным фондом и анализа финансовых результатов гостиницы.

## Структура
- `HotelPMS.API` — сервер (ASP.NET Core Web API, EF Core, SQL Server LocalDB)
- `HotelPMS.Client` — клиент WPF (MVVM), общается с сервером только по HTTP
- `HotelPMS.Tests` — тесты (xUnit)

## Запуск
1. Package Manager Console, проект по умолчанию `HotelPMS.API`: `Add-Migration Initial`
2. Запустить `HotelPMS.API` (профиль https, порт 7296). В Development миграции применяются автоматически.
3. Описание API: `https://localhost:7296/openapi/v1.json`
4. Запустить `HotelPMS.Client`. Адрес сервера — `HotelPMS.Client/Services/ApiClient.cs` (`DefaultBaseUrl`).
   Удобно: Решение → Настроить запускаемые проекты → несколько проектов (API и Client).

## Этапы
- [x] Каркас сервера и БД
- [x] Каркас клиента (номера, бронирование, счёт, KPI)
- [ ] График динамики доходов
- [ ] Авторизация и роли
- [ ] Тесты расчёта KPI
- [ ] Пояснительная записка

## Формулы
- Occupancy = проданные номеро-ночи / доступные номеро-ночи × 100
- ADR = выручка от номеров / проданные номеро-ночи
- RevPAR = выручка от номеров / доступные номеро-ночи
