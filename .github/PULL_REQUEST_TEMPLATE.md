## Описание

<!-- Кратко: что делает этот PR и зачем -->

## Тип изменения

- [ ] Bug fix
- [ ] New feature
- [ ] Performance
- [ ] Refactoring
- [ ] Documentation
- [ ] Tests
- [ ] Build/CI

## Чеклист

- [ ] `dotnet build ChaosLab.sln -c Release` — без warnings
- [ ] `dotnet test` — все тесты зелёные
- [ ] `.editorconfig` соблюдён
- [ ] Коммиты в стиле [Conventional Commits](https://www.conventionalcommits.org/)
- [ ] Обновлены соответствующие `.md` в `docs/` (если нужно)
- [ ] Комментарии XML добавлены к публичным членам (CS1591 не должен появляться)

### Если новая система

- [ ] Зарегистрирована в `Core/SystemCatalog.cs`
- [ ] Добавлена в `Systems/EquationsCatalog.cs`
- [ ] Указаны оси для `DefaultLyapunovAxes`
- [ ] Написан аналитический `JacobianVectorProduct`
- [ ] Есть тест с проверкой λ₁ на эталонных параметрах
- [ ] Добавлена статья в `docs/articles/systems/`

### Если новый метод анализа

- [ ] Есть ссылка на первоисточник в XML-комментарии
- [ ] Есть тест с эталонным значением
- [ ] Добавлена кнопка в `AnalysisViewModel`
- [ ] Добавлена статья в `docs/articles/methods/`
- [ ] Добавлен в общий `RunAllAsync` (если применимо)

## Скриншоты

<!-- Для UI-изменений: приложите "до" и "после" -->

| До | После |
|----|-------|
|    |       |

## Связанные issues

Closes #

## Дополнительный контекст

<!-- Любая информация, которая поможет ревьюеру -->
