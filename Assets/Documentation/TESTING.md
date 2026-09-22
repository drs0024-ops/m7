# Testing — BoneYardEscape

**Framework:** NUnit (Unity Test Framework 1.6.0)
**Mode:** Edit Mode only (no Play Mode tests)
**Runner:** Window → General → Test Runner → Edit Mode tab

---

## Running Tests

1. Open **Window → General → Test Runner**
2. Select the **Edit Mode** tab
3. Click **Run All** (green play button, top-right)
4. Look for: **N Passed · 0 Failed · 0 Errors** in the bottom-right panel

**Run a single test:** Right-click the test in the left panel → **Run**

**Run a single fixture:** Right-click the class name → **Run**

---

## Assembly Setup

**File:** `Assets/Tests/EditMode/Tests.EditMode.asmdef`

```json
{
    "name": "Tests.EditMode",
    "rootNamespace": "Game.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Bootstrap",
        "Core",
        "Gameplay",
        "MessagePipe"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": true,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}

Critical references:

UnityEngine.TestRunner + UnityEditor.TestRunner — without these, the Test Runner won't recognize the assembly
defineConstraints: ["UNITY_INCLUDE_TESTS"] — ensures the assembly only compiles in test contexts
Current Test Coverage
Fixture	Tests	What it verifies
GameStateMachineTests	15	Transition table: valid transitions succeed, invalid rejected, ForceState bypasses, CanTransitionTo correct
SaveMigrationTests	6	Save versioning: v0 deserializes, migration bumps version, data preserved, missing keys graceful, round-trip stable, current version skips migration
Total	21	

Test Patterns
Pattern 1: Plain C# Class (Reflection-Injected Publishers)
Use when the class under test has IPublisher<T> fields but you don't need actual message delivery.

[TestFixture]
public class GameStateMachineTests
{
    private GameStateMachine _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new GameStateMachine();
        InjectNoOpPublishers();
    }

    [Test]
    public void TransitionTo_MainMenuToLoading_Succeeds()
    {
        Assert.That(_sut.CurrentState, Is.EqualTo(GameState.MainMenu));
        Assert.That(_sut.TransitionTo(GameState.Loading), Is.True);
        Assert.That(_sut.CurrentState, Is.EqualTo(GameState.Loading));
    }

    #region Helpers

    private void InjectNoOpPublishers()
    {
        var fields = typeof(GameStateMachine).GetFields(
            BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!field.FieldType.IsGenericType) continue;
            if (field.FieldType.GetGenericTypeDefinition() != typeof(IPublisher<>)) continue;

            var implType = typeof(NoOpPublisher<>).MakeGenericType(
                field.FieldType.GetGenericArguments());
            field.SetValue(_sut, Activator.CreateInstance(implType));
        }
    }

    private class NoOpPublisher<T> : IPublisher<T>
    {
        public void Publish(T message) { }
    }

    #endregion
}

When to use: Unit tests for state machines, controllers, or any class where you only care about return values and state changes, not message side-effects.

Why not VContainer: BuiltinContainerBuilder doesn't auto-resolve IPublisher<T>. VContainer's ContainerBuilder + RegisterMessagePipe + RegisterBuildCallback works but is overkill for unit tests. Reflection is faster and has zero dependencies.

Pattern 2: Pure Data (No Unity, No DI)
Use for save format, serialization, migration, or any pure logic test.

[TestFixture]
public class SaveMigrationTests
{
    [Test]
    public void Migrate_V0ToV1_BumpsVersion()
    {
        var data = new SaveDataContainer();
        data.SetValue("LevelProgression", "{\"levelName\":\"Level_01\"}");

        SaveMigration.Migrate(data, data.version, 1);

        Assert.That(data.version, Is.EqualTo(1));
    }
}

When to use: Tests that only touch [Serializable] POCOs, static methods, or JsonUtility round-trips. No MonoBehaviour, no IStartable, no publishers.

Pattern 3: MonoBehaviour (Future — Play Mode)
Not currently used. If you add Play Mode tests later (e.g., integration smoke tests), the pattern would be:

[UnityTestFixture]
public class IntegrationTests : MonoBehaviour
{
    [UnityTest]
    public IEnumerator FullBootSequence()
    {
        // Spawn ProjectLifetimeScope, wait frames, assert state
        yield return null;
    }
}

This would require a Tests.PlayMode.asmdef with defineConstraints: ["UNITY_INCLUDE_TESTS"] and the same TestRunner references.

Adding a New Test
Step 1: Create the file
Assets/Tests/EditMode/YourSystemTests.cs

Step 2: Choose the pattern
Your system has...	Use pattern
IPublisher<T> fields, plain C#	Pattern 1 (reflection)
Pure data / static methods	Pattern 2 (no setup)
MonoBehaviour + scene dependencies	Pattern 3 (Play Mode, future)

Step 3: Write tests
Naming convention: MethodName_Condition_ExpectedResult

[Test]
public void TransitionTo_MainMenuToPaused_Rejected() { ... }

[Test]
public void Migrate_PreservesAllOriginalKeys() { ... }

[Test]
public void RoundTrip_V1_SerializeDeserialize_DataIntact() { ... }

Step 4: Run and verify
Test Runner → Edit Mode → Run All
New tests appear under your fixture name
All green → done
Known Limitations
Limitation	Workaround
JsonUtility requires [Serializable] on nested classes in lists	Add [System.Serializable] to any nested class used in a List<T> field
JsonUtility is strict about JSON format	Build test data programmatically (SetValue), not via hand-crafted JSON strings
No integration tests	Individual units are tested, but the wiring between them (message flow, DI resolution, scene loading) is not automated
No CI/CD	Tests run manually in the editor. GitHub Actions setup is the next step

Troubleshooting
Symptom	Cause	Fix
Test Runner shows "Create a new test assembly" buttons	Missing UnityEngine.TestRunner / UnityEditor.TestRunner in asmdef references	Add both to references array
Tests listed but show empty circles	Tests haven't been run yet	Click Run All
"No match found" errors	IPublisher<T> not resolvable	Use reflection pattern (Pattern 1), not VContainer
Tests not appearing after adding file	Wrong folder or stale cache	File must be in Assets/Tests/EditMode/. Right-click folder → Reimport
IStartable not found	Missing using VContainer;	Add the using directive
BuiltinContainerBuilder not found	Wrong MessagePipe init approach	Don't use it. Use reflection or VContainer ContainerBuilder

File Locations
Assets/Tests/EditMode/
  ├── Tests.EditMode.asmdef
  ├── GameStateMachineTests.cs
  └── SaveMigrationTests.cs

Conventions
Namespace: Game.Tests
Fixture naming: [SystemName]Tests (e.g., GameStateMachineTests, SaveMigrationTests)
Test naming: MethodName_Condition_ExpectedResult
No using UnityEngine.TestTools; unless writing Play Mode tests
No file I/O in tests — use in-memory strings for JSON
No network calls, no Unity API that requires a scene (Edit Mode has no active scene)
Assert on behavior, not implementation — check CurrentState, not internal field values

Save it as `Assets/Documentation/TESTING.md` (or wherever your docs folder lives). It's sel