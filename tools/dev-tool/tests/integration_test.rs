use std::process::Command;

#[test]
fn test_version_output() {
    let output = Command::new("cargo")
        .args(["run", "--", "--version"])
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("dev-tool"));
}

#[test]
fn test_help_output() {
    let output = Command::new("cargo")
        .args(["run", "--", "--help"])
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("Yazi-integrated Rust CLI"));
    assert!(stdout.contains("--server"));
    assert!(stdout.contains("--token"));
    assert!(stdout.contains("--output"));
    assert!(stdout.contains("--timeout"));
    assert!(stdout.contains("connect"));
    assert!(stdout.contains("send"));
    assert!(stdout.contains("list"));
    assert!(stdout.contains("config"));
}

#[test]
fn test_config_command() {
    let output = Command::new("cargo")
        .args(["run", "--", "config"])
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("Current Configuration"));
    assert!(stdout.contains("Server:"));
    assert!(stdout.contains("ws://127.0.0.1:5007/gm"));
}

#[test]
fn test_config_json_output() {
    let output = Command::new("cargo")
        .args(["run", "--", "--output", "json", "config"])
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("\"server\""));
    assert!(stdout.contains("\"timeout\""));
}

#[test]
fn test_env_var_server() {
    let output = Command::new("cargo")
        .args(["run", "--", "config"])
        .env("DEV_TOOL_SERVER", "ws://test:9000")
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("ws://test:9000"));
}

#[test]
fn test_cli_override_server() {
    let output = Command::new("cargo")
        .args(["run", "--", "--server", "ws://cli:8000", "config"])
        .env("DEV_TOOL_SERVER", "ws://env:9000")
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("ws://cli:8000"));
}

#[test]
fn test_connect_without_server() {
    let output = Command::new("cargo")
        .args(["run", "--", "connect"])
        .output()
        .expect("Failed to execute command");

    // Connection will fail without a server running
    assert!(!output.status.success());
    let stderr = String::from_utf8_lossy(&output.stderr);
    let stdout = String::from_utf8_lossy(&output.stdout);

    // Should show connection attempt
    assert!(stdout.contains("Connecting to:") || stderr.contains("Failed to connect"));
}

#[test]
fn test_send_stub() {
    let output = Command::new("cargo")
        .args(["run", "--", "send", "test message"])
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("Sending message:"));
    assert!(stdout.contains("not yet implemented"));
}

#[test]
fn test_list_stub() {
    let output = Command::new("cargo")
        .args(["run", "--", "list"])
        .output()
        .expect("Failed to execute command");

    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("Available operations:"));
    assert!(stdout.contains("not yet implemented"));
}
