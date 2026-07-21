# Результат

<video src="https://github.com/user-attachments/assets/9162210d-d813-421d-a04e-a164b88c55ac" width="100%" controls playsinline></video>

# NMService

Централизованный сервис для управления сетевыми сообщениями, реализующий паттерн "Издатель-Подписчик" с клиент-серверной фильтрацией. Позволяет клиентам подписываться на конкретные типы сообщений, а серверу — отправлять их только заинтересованным клиентам.

## Поток управления

```mermaid
sequenceDiagram
    participant DI as DI-контейнер<br/>(использует IDisposable)
    participant NM as NetworkManager<br/>(использует INMLifecycle)
    participant Client as Клиент<br/>(использует INMClient)
    participant Server as Сервер<br/>(использует INMServer)
    participant Svc as NMService<br/>(реализует INMLifecycle, INMClient, INMServer, IDisposable)
    
    NM->>Svc: InitializeServer()
    NM->>Svc: InitializeClient()
    Client->>Svc: (Subscribe/Unsubscribe)Client<T>()
    Server->>Svc: Raise<T>()
    NM->>Svc: ClearClientSubscriptionOnDisconnect()
    DI->>Svc: Dispose()
``` 
## Интерфейсы

| Интерфейс | Кто использует | Назначение |
|-----------|----------------|------------|
| `INMLifecycle` | NetworkManager | Управление жизненным циклом: инициализация обработчиков и очистка подписок при отключении клиентов |
| `INMServer` | Серверные скрипты | Отправка событий клиентам |
| `INMClient` | Клиентские скрипты | Клиентские подписки на события |
| `IDisposable` | DI-контейнер | Очистка ресурсов |
