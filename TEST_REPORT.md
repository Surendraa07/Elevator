# Elevator Simulator – Test Report

**Application:** `ElevatorSystem` (C# .NET 8 Windows Forms, x64)
**Test date:** 2026-10-11
**Test machine:** Windows 10 Home 22H2 (x64), .NET SDK 10.0.401 targeting `net8.0-windows`, Microsoft ACE OLE DB 16.0 (64-bit, Office Click-to-Run)
**Build:** `dotnet build Elevator.sln` (Debug and Release): 0 warnings, 0 errors

## How the tests were run

| Method | Used for |
|---|---|
| **Automated harness**: a throwaway console program that references `ElevatorSystem` and drives `Elevator`, `Dispatcher` and `ElevatorLogRepository` directly while pumping the WinForms message loop, so the real `System.Windows.Forms.Timer` animation runs | State machine, dispatcher, database and error-case tests |
| **UI automation**: the real `ElevatorSystem.exe`, with buttons pressed through Windows UI Automation and window screenshots taken | GUI, animation, log window, locked-database behaviour |
| **Database inspection**: an OleDb query against `bin\x64\Debug\net8.0-windows\Data\ElevatorLog.accdb` after the app closed | Logged rows, final flush on exit |
| **Code inspection**: search of the source | "No ExecuteNonQuery inserts", "no Thread.Sleep", "DB code only in one class" |

Timing used throughout: travel 3.0 s per floor, doors 1.2 s to open or close, doors held open 2.0 s, animation tick 30 ms.

---

## 1. Test plan

### Phase 1 – GUI and control logic

| ID | Description | Steps | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|---|
| T1-01 | Main window layout | Start app, pick a mode, press Start | Request button and status display for each floor, car panel with buttons 0 and 1 and a floor display, shaft with car and doors, Log button | All present in both modes (single: 722×629 window, two: 1182×629) | Pass |
| T1-02 | Car button moves the car | Idle at floor 0, press car button **1** | Button lights. Car moves up, stops, doors open, stay about 2 s, close. Button resets and displays show floor 1 | Events in order: `CarRequest → MoveStarted → Arrived → DoorsOpening → DoorsOpened → RequestServed → DoorsClosing → DoorsClosed → Idle`. Full cycle took 7.5 s. Car at floor 1, doors closed, button unlit | Pass |
| T1-03 | Request button calls the car | Car at 0, press **▼ Request elevator** on floor 1 | Button lights, car goes to floor 1 and opens, button resets | As expected. Status bar: "Request at floor 1 → Elevator 1" | Pass |
| T1-04 | Request for the current floor | Car idle at 0, press floor 0's request button | Doors open and close, car does not move | `HallRequest → DoorsOpening → DoorsOpened → RequestServed → DoorsClosing → DoorsClosed → Idle`, no `MoveStarted` | Pass |
| T1-05 | Request while moving | Press car **1**. After 1.5 s (moving) press **0**, then **1** again | Trip to 1 is not interrupted. 0 is queued and served afterwards. Pressing 1 again (current target) is ignored | Queue after the presses = `[0]`. Car arrived at 1 then at 0 (2 arrivals), ended idle at 0 | Pass |
| T1-06 | Same-floor request while doors are closing | Let doors start closing at floor 0, press floor 0's request button | Doors reopen from their current position | State went from `Doors closing` back to `Doors opening`. `DoorsOpened` logged twice | Pass |
| T1-07 | Car button while doors are open | Doors open at 0, press car **1** | Doors close straight away (no 2 s wait) and car leaves | State went to `Doors closing` immediately, car reached floor 1 | Pass |
| T1-08 | Pressing a lit request button again | Press floor 0's request button twice while waiting | Second press is ignored and keeps the same assignment | Same elevator returned, no second request queued | Pass |
| T1-09 | Displays update | Watch the floor displays and car display during a trip | Floor displays show `▲ going to 1`, then `Floor 1 opening` / `open` / `closing`, then `Floor 1 ◄`. Car display shows floor and arrow | As expected (screenshots) | Pass |
| T1-10 | Buttons highlight and reset | Press any request or car button | Gold when pressed, grey again once doors open at that floor | As expected | Pass |
| T1-11 | Theme selection | In the startup dialog pick **Space station** (default) or **Classic office building** | Space: starfield with planet, translucent panels, glass-tube shafts, hazard-striped decks, airlock doors with red (sealed) / green (open) lights, cyan displays, amber lit buttons, dark log grid. Classic: original look. Behaviour identical in both | Both themes verified with screenshots during a trip and in the log window. Lit, reset and door animation work in both | Pass |

### Phase 2 – Logging and data management

| ID | Description | Steps | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|---|
| T2-01 | **Missing database** at startup | Delete `Data\` in the output folder, start | ADOX creates `Data\ElevatorLog.accdb` and table `ElevatorLog` (Id AutoNumber PK, EventTime DateTime, ElevatorId, Floor, Status Text 255). Status bar shows "(created)" | `EnsureDatabase()` returned `true` and the file exists. Columns read back: `Id:Int32, EventTime:DateTime, ElevatorId:Int32, Floor:Int32, Status:String` | Pass |
| T2-02 | Existing database is reused | Start again | Nothing is recreated and the history is kept | `EnsureDatabase()` returned `false`. Earlier rows still present | Pass |
| T2-03 | Every event is logged with a timestamp | Do a trip, close the app, query the table | Rows for request, move start, arrival, doors opening/open/closing/closed, served, idle, plus system start/close | e.g. rows 50–59: `CarRequest`, `MoveStarted`, `Arrived`, `DoorsOpening`, `DoorsOpened`, `RequestServed`, `DoorsClosing`, `DoorsClosed`, `Idle`, `System: simulator closed`, all timestamped | Pass |
| T2-04 | Log is shown in a DataGridView | Press **Log** | Grid with Id, Time, Elevator, Floor, Event/status, newest first | "39 entries (newest first) – Data\ElevatorLog.accdb", columns formatted | Pass |
| T2-05 | Disconnected model for inserts | Insert 3 entries through `InsertLogs` | Rows are added to a DataSet's DataTable and saved with `OleDbDataAdapter.Update`, INSERT generated by `OleDbCommandBuilder` | 3 rows read back, AutoNumber Ids ascending. Source search: no `ExecuteNonQuery` anywhere | Pass |
| T2-06 | Relative path | Inspect connection string, run from Debug and Release folders | Path built from `Application.StartupPath` + `Data\ElevatorLog.accdb`. The `.accdb` is copied to each output folder | `bin\x64\Debug\…\Data\ElevatorLog.accdb` and `bin\x64\Release\…\Data\ElevatorLog.accdb` both present and used | Pass |
| T2-07 | **Locked database** at startup | Open the `.accdb` with an exclusive lock (`FileShare.None`), start the app, press car **1**, release the lock | Friendly warning. Elevators keep working. Events are kept, then saved once the file is free | Message: *"Could not prepare the log database. The database file 'Data\ElevatorLog.accdb' is locked by another program. Close it (for example in MS Access) and try again. The elevators keep working…"*. Car completed its trip. After unlocking, the status bar showed "logging resumed" and all events from the locked period were in the table | Pass |
| T2-08 | **Locked database** when reading the log | Exclusive lock, then load the log | Friendly warning, no crash | *"Could not load the operation log. The database file 'Data\ElevatorLog.accdb' is locked by another program…"* | Pass |
| T2-09 | **Database deleted while running** | Delete the `.accdb`, then log another event | File and table are recreated, logging continues | Recreated, 1 row present afterwards | Pass |
| T2-10 | **Corrupt database file** | Replace the `.accdb` with a text file, load the log | Friendly message suggesting deleting the file | *"…is damaged or is not an Access database. Delete it and restart the program to create a new one."* | Pass |
| T2-11 | Over-long status text | Insert a 400-character status | Truncated to the 255-character column, no error | Stored length 255 | Pass |
| T2-12 | ACE provider not installed | Run on a PC without the 64-bit Access Database Engine | Friendly message naming the missing provider, elevators still work | **Not run**: the test PC has the provider installed. The message mapping exists in `ElevatorLogRepository.Translate` but was not exercised | Not tested |
| T2-13 | Pending log saved on exit | Use the elevator, close the window right away | Window waits for the background save, then closes. Last row is "System: simulator closed" | Row 48/59 "System: simulator closed" present and process exited | Pass |
| T2-14 | No duplicated DB code | Code search for `OleDbConnection`, `OleDbDataAdapter` | Found only in `Data/ElevatorLogRepository.cs` | Only that file | Pass |

### Phase 3 – Animation

| ID | Description | Steps | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|---|
| T3-01 | Car movement is animated | Press car **1** and watch | Car slides smoothly between floors, driven by `System.Windows.Forms.Timer` (30 ms) | After 1.5 s the state was `Moving` with a fractional position. Arrival took about 3 s | Pass |
| T3-02 | Doors animate together | Watch arrival | Car door and floor door open and close by the same amount | Screenshot during `Doors opening` shows both doors part-open by the same width | Pass |
| T3-03 | Steps are sequenced by events | Check event order (T1-02) | `ElevatorArrived` starts doors opening, `DoorsOpened` starts the dwell, `DoorsClosed` starts the next stop | Order matched exactly | Pass |
| T3-04 | No blocking on the UI thread | Code search for `Thread.Sleep`; drag the window during a trip | No `Thread.Sleep` and the UI stays responsive | None in the project. UI responsive, buttons work mid-trip (T1-05) | Pass |

### Phase 4 – Concurrency

| ID | Description | Steps | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|---|
| T4-01 | DB creation/check off the UI thread | Start with no database | Window usable immediately, DB prepared on a `BackgroundWorker`, status updated in `RunWorkerCompleted` | As expected | Pass |
| T4-02 | Log writes in the background | Run several trips | Entries queued and written in batches every 0.5 s on a `BackgroundWorker`, no stutter | Smooth animation, all rows saved | Pass |
| T4-03 | Log read in the background | Press **Log** / **Refresh** | Queue flushed first, then the table loads on a `BackgroundWorker` and is bound in `RunWorkerCompleted`. Refresh is disabled while loading | As expected, grid included the latest events | Pass |
| T4-04 | Retry after a write failure | Lock the DB during use, then unlock | Failed batch goes back to the front of the queue (order kept) and is retried every 5 s. Message box shown only once | As T2-07. A modal message box does not stop retries | Pass |

### Phase 5 – Two-elevator system

| ID | Description | Steps | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|---|
| T5-01 | Mode selection | Start, pick "Single elevator" or "Two elevators" | One or two shafts and car panels | Both modes verified (screenshots) | Pass |
| T5-02 | Car requests stay in their own car | Two-elevator mode, press E2's car **1** | Only Elevator 2 moves | Only E2 moved | Pass |
| T5-03 | Dispatcher picks the shortest response time | E1 moving 0→1, press floor 0's request button | Goes to E2, which is idle at floor 0 | Assigned to E2 (ETA 1.2 s against E1's queue) | Pass |
| T5-04 | Current task is never interrupted | Same as T5-03 | E1 keeps its target | E1's target stayed floor 1 | Pass |
| T5-05 | Car already at the floor with doors open | E2 open at floor 1, press floor 1's request button | Goes to E2 (ETA 0), served at once, button resets | "Request at floor 1 → Elevator 2", button reset at once | Pass |
| T5-06 | Request button resets when served | Watch the dispatcher's light events | `on` then `off` for each call | `0:on(E2) 0:off(E2) 1:on(E1) 1:off(E1)` | Pass |
| T5-07 | State pattern: each state handles requests differently | T1-04 to T1-07 | Idle: open or start moving. Moving: queue. DoorsOpening: queue or wait. DoorsOpen: hold doors, or close at once for a car request. DoorsClosing: reopen or queue | All behaviours observed | Pass |
| T5-08 | Observer pattern | One car trip in two-elevator mode | Floor displays, car panel, logger and dispatcher all react to the same events | All updated, rows logged, hall light reset | Pass |

### Phase 6 – Documentation

| ID | Description | Expected result | Actual result | Pass/Fail |
|---|---|---|---|---|
| T6-01 | Test report | Test plan, self-assessment and class diagram | This document | Pass |

---

## 2. Self-assessment

| Task | Sub-task | Possible marks | Completed (Yes/No) | Test ID reference |
|---|---|---|---|---|
| T1 | GUI (request buttons, car panel, floor displays, shaft, Log button) | 10 | Yes | T1-01, T1-09, T1-10, T1-11 |
| T1 | Event handler skeletons | 10 | Yes | T1-01 to T1-03 |
| T2 | Event handlers work (movement, doors, current-floor and in-motion requests) | 10 | Yes | T1-02 to T1-08 |
| T3 | Database connected (ADOX creation, OleDb) | 5 | Yes | T2-01, T2-02 |
| T3 | Log displayed (DataGridView) | 5 | Yes | T2-04, T4-03 |
| T3 | Log stored | 5 | Yes | T2-03, T2-13 |
| T3 | DataAdapter.Update (DataSet + OleDbCommandBuilder) | 5 | Yes | T2-05 |
| T3 | Relative paths / no logic errors / try-catch | 5 | Yes | T2-06 to T2-11 |
| T3 | No DB code duplication | 5 | Yes | T2-14 |
| T4 | Animation with delegates and timer | 10 | Yes | T3-01 to T3-04 |
| T5 | BackgroundWorker | 5 | Yes | T4-01 to T4-04 |
| T5 | Two elevators with State and Observer patterns | 15 | Yes | T5-01 to T5-08 |
| T6 | Test report | 10 | Yes | T6-01 |
| | **Total** | **100** | | |

Not tested: T2-12 (no ACE provider). Needs a PC without the Access Database Engine.

---

## 3. Class diagram (State and Observer design)

```
                              «interface»
                            IElevatorState
             ───────────────────────────────────────────────
             + Name : string
             + Enter(Elevator)
             + HandleRequest(Elevator, floor, RequestSource)
             + Tick(Elevator, elapsedMs)
             + RemainingTaskMs(Elevator) : double
                                 △
         ┌──────────────┬────────┴───────┬──────────────────┬───────────────────┐
     IdleState     MovingState   DoorsOpeningState   DoorsOpenState   DoorsClosingState
     (singletons; per-car data lives on Elevator)

  ┌──────────────────────────────────────────┐   State ┌──────────────────┐
  │ Elevator  (context + subject)            │────────►│ IElevatorState   │
  │──────────────────────────────────────────│         └──────────────────┘
  │ Id, CurrentFloor, Position, TargetFloor  │
  │ DoorOpenRatio, Direction, PendingStops   │  observers 0..*  «interface»
  │ - timer : Forms.Timer                    │◇───────────────► IElevatorObserver
  │ + RequestFloor(floor, source)            │                  + Update(Elevator, ElevatorEventArgs)
  │ + EstimateResponseTimeMs(floor)          │                             △
  │ + Attach / Detach(IElevatorObserver)     │     ┌───────────────┬───────┴───────┬───────────────┐
  │ event ElevatorArrived, DoorsOpened,      │  FloorDisplay   CarPanel        LogObserver     Dispatcher
  │       DoorsClosed, MoveStarted           │  Observer       Observer           │            │ assigns hall calls
  │       (ElevatorEventHandler delegate)    │  (Label)        (CarPanelControl)  │            │ to Elevator[1..2]
  │ event Animated  ──► ElevatorShaftControl │                                    ▼            ▼
  └──────────────────────────────────────────┘                     BackgroundLogWriter     HallCallChanged ──► MainForm
                                                                   (BackgroundWorker)      (request button lights)
                                                                          │
                                                                          ▼
                                                                ElevatorLogRepository  ── only DB class
                                                                (ADOX create, DataSet,
                                                                 OleDbDataAdapter.Update,
                                                                 OleDbCommandBuilder)
```

**State pattern.** `Elevator` is the context. It keeps one `IElevatorState` and passes `HandleRequest` and timer `Tick` calls to it, so the per-state rules live in the state classes instead of a large `switch`:

- **Idle**: a request for this floor opens the doors, any other request starts moving.
- **Moving**: requests are only queued. The car never reverses mid-trip.
- **DoorsOpening**: a request for this floor waits for the doors to finish opening, other floors are queued.
- **DoorsOpen**: a request for this floor is served at once and the doors stay open longer. A car button for another floor closes the doors straight away, while a hall request waits out the 2 s.
- **DoorsClosing**: a request for this floor reopens the doors, other floors are queued.

States are stateless singletons, so both elevators share them. Each `RemainingTaskMs` feeds the dispatcher's estimate.

**Sequencing with delegates and events.** When a step finishes, the state raises an event through `Elevator.Raise`: `ElevatorArrived`, `DoorsOpened` or `DoorsClosed` (all of the `ElevatorEventHandler` delegate type). The elevator's own handlers for those events start the next step: arrival leads to doors opening, open doors lead to the dwell, and closed doors lead to the next stop or Idle. A `System.Windows.Forms.Timer` drives the animation, so nothing blocks the UI thread.

**Observer pattern.** `Elevator` is also the subject. `Raise` first sends every `ElevatorEventArgs` to all attached `IElevatorObserver`s, then fires the matching event, so the log order always matches what happened. The observers are:

- **FloorDisplayObserver** (one per floor): shows where every car is.
- **CarPanelObserver** (one per car): updates the floor display and lights or resets the floor buttons.
- **LogObserver**: queues a `LogEntry` for the `BackgroundLogWriter`, which saves batches on a `BackgroundWorker` through `ElevatorLogRepository`.
- **Dispatcher**: watches for `RequestServed` to reset request buttons.

The `Dispatcher` gives each request-button press to the elevator with the lowest `EstimateResponseTimeMs`. The request is appended to that car's queue, so the car's current task is never interrupted. Car-panel presses go straight to their own car and never reach the dispatcher.

## 4. Known limitations

- If a different car opens its doors at a floor first, the request button resets. The originally assigned car still makes its queued stop, since the dispatcher never cancels a task already given to a car.
- Times are stored to the second, because Access rejects OLE DB timestamps with fractions of a second. Entries stay in order through the AutoNumber `Id`.
- A Visual Studio build uses the `COMReference` to ADOX directly. A `dotnet build` uses the same type library pre-imported as `lib\Interop.ADOX.dll`, because the .NET Core MSBuild cannot resolve COM references (MSB4803).
