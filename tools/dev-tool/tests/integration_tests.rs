use assert_cmd::Command;
use predicates::prelude::*;
use serde_json::Value;

#[test]
fn test_spawn_json_format() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    let output = cmd
        .args(&[
            "spawn", "--mob", "goblin", "--x", "10", "--y", "20", "--output", "json",
        ])
        .output()
        .unwrap();

    assert!(output.status.success());

    let json: Value = serde_json::from_slice(&output.stdout).unwrap();
    assert_eq!(json["cmd"], "spawn");
    assert_eq!(json["args"]["mob"], "goblin");
    assert_eq!(json["args"]["x"], 10);
    assert_eq!(json["args"]["y"], 20);
    assert_eq!(json["status"], "success");
}

#[test]
fn test_spawn_text_format() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.args(&["spawn", "--mob", "goblin", "--x", "10", "--y", "20"])
        .assert()
        .success()
        .stdout(predicate::str::contains(
            "Spawning mob 'goblin' at coordinates (10, 20)",
        ));
}

#[test]
fn test_spawn_empty_mob_error() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.args(&["spawn", "--mob", "", "--x", "10", "--y", "20"])
        .assert()
        .failure()
        .stderr(predicate::str::contains("Mob name cannot be empty"));
}

#[test]
fn test_tp_json_format() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    let output = cmd
        .args(&["tp", "--x", "5", "--y", "15", "--output", "json"])
        .output()
        .unwrap();

    assert!(output.status.success());

    let json: Value = serde_json::from_slice(&output.stdout).unwrap();
    assert_eq!(json["cmd"], "tp");
    assert_eq!(json["args"]["x"], 5);
    assert_eq!(json["args"]["y"], 15);
    assert_eq!(json["status"], "success");
}

#[test]
fn test_reload_json_format() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    let output = cmd.args(&["reload", "--output", "json"]).output().unwrap();

    assert!(output.status.success());

    let json: Value = serde_json::from_slice(&output.stdout).unwrap();
    assert_eq!(json["cmd"], "reload");
    assert_eq!(json["status"], "success");
    // reload should not have args
    assert!(json["args"].is_null());
}

#[test]
fn test_regen_map_with_seed_json_format() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    let output = cmd
        .args(&["regen-map", "--seed", "12345", "--output", "json"])
        .output()
        .unwrap();

    assert!(output.status.success());

    let json: Value = serde_json::from_slice(&output.stdout).unwrap();
    assert_eq!(json["cmd"], "regen-map");
    assert_eq!(json["args"]["seed"], 12345);
    assert_eq!(json["status"], "success");
}

#[test]
fn test_regen_map_without_seed_json_format() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    let output = cmd
        .args(&["regen-map", "--output", "json"])
        .output()
        .unwrap();

    assert!(output.status.success());

    let json: Value = serde_json::from_slice(&output.stdout).unwrap();
    assert_eq!(json["cmd"], "regen-map");
    assert_eq!(json["status"], "success");
    // regen-map without seed should have empty args object
    assert!(json["args"].is_object());
    assert_eq!(json["args"].as_object().unwrap().len(), 0);
}

#[test]
fn test_exit_code_success() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.args(&["reload"]).assert().success().code(0);
}

#[test]
fn test_exit_code_error() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.args(&["spawn", "--mob", "", "--x", "10", "--y", "20"])
        .assert()
        .failure()
        .code(1);
}

#[test]
fn test_help_displays() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.arg("--help")
        .assert()
        .success()
        .stdout(predicate::str::contains("Development tool for Pigeon Pea"));
}

#[test]
fn test_exit_code_missing_required_args() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.args(&["spawn", "--mob", "goblin"])
        .assert()
        .failure()
        .code(2);
}

#[test]
fn test_spawn_whitespace_only_mob() {
    let mut cmd = Command::cargo_bin("dev-tool").unwrap();
    cmd.args(&["spawn", "--mob", "   ", "--x", "10", "--y", "20"])
        .assert()
        .failure()
        .code(1)
        .stderr(predicate::str::contains("Mob name cannot be empty"));
}
