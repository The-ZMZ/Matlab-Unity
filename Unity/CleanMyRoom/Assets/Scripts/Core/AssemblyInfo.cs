using System.Runtime.CompilerServices;

// The EditMode architecture tests exercise the lifecycle helpers that are
// called by Unity from Awake/OnDestroy. Granting access to that one test
// assembly keeps those helpers internal everywhere else.
[assembly: InternalsVisibleTo("CleanMyRoom.Tests.SceneArchitecture")]
