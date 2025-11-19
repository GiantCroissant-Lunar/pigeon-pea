//! Command implementations for dev-tool
//!
//! This module contains all command implementations that can be executed
//! by the dev-tool CLI.

use crate::client::CommandExecutor;
use crate::error::Result;
use async_trait::async_trait;

pub mod spawn;
pub mod teleport;
pub mod reload;
pub mod regen_map;
pub mod load_map;
pub mod send;
pub mod state;
pub mod log;

// Re-export all command structs for convenience
pub use spawn::SpawnCommand;
pub use teleport::TeleportCommand;
pub use reload::ReloadCommand;
pub use regen_map::RegenMapCommand;
pub use load_map::LoadMapCommand;
pub use send::SendCommand;
pub use state::StateCommand;
pub use log::LogCommand;

/// Helper function to validate coordinates
pub fn validate_coordinates(x: i32, y: i32) -> Result<()> {
    // Basic coordinate validation - can be extended
    if x < -100000 || x > 100000 {
        return Err(crate::error::CliError::InvalidInput(
            format!("X coordinate {} is out of valid range [-100000, 100000]", x)
        ));
    }

    if y < -100000 || y > 100000 {
        return Err(crate::error::CliError::InvalidInput(
            format!("Y coordinate {} is out of valid range [-100000, 100000]", y)
        ));
    }

    Ok(())
}

/// Helper function to validate file path
pub fn validate_file_path(path: &str) -> Result<()> {
    if path.is_empty() {
        return Err(crate::error::CliError::InvalidInput(
            "File path cannot be empty".to_string()
        ));
    }

    // Basic path validation - can be extended with more checks
    if path.contains("..") {
        return Err(crate::error::CliError::InvalidInput(
            "File path cannot contain '..' for security reasons".to_string()
        ));
    }

    Ok(())
}

/// Helper function to validate entity/mob type
pub fn validate_mob_type(mob: &str) -> Result<()> {
    if mob.is_empty() {
        return Err(crate::error::CliError::InvalidInput(
            "Mob type cannot be empty".to_string()
        ));
    }

    // Basic validation - can be extended with known mob types
    if mob.len() > 100 {
        return Err(crate::error::CliError::InvalidInput(
            "Mob type name is too long (max 100 characters)".to_string()
        ));
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_validate_coordinates_valid() {
        assert!(validate_coordinates(0, 0).is_ok());
        assert!(validate_coordinates(100, 200).is_ok());
        assert!(validate_coordinates(-50, -75).is_ok());
    }

    #[test]
    fn test_validate_coordinates_invalid() {
        assert!(validate_coordinates(100001, 0).is_err());
        assert!(validate_coordinates(0, 100001).is_err());
        assert!(validate_coordinates(-100001, 0).is_err());
        assert!(validate_coordinates(0, -100001).is_err());
    }

    #[test]
    fn test_validate_file_path_valid() {
        assert!(validate_file_path("data/maps/test.json").is_ok());
        assert!(validate_file_path("/absolute/path/to/map.json").is_ok());
        assert!(validate_file_path("relative\\path\\windows.json").is_ok());
    }

    #[test]
    fn test_validate_file_path_invalid() {
        assert!(validate_file_path("").is_err());
        assert!(validate_file_path("../outside/allowed/path").is_err());
        assert!(validate_file_path("path/../with/../dots.json").is_err());
    }

    #[test]
    fn test_validate_mob_type_valid() {
        assert!(validate_mob_type("goblin").is_ok());
        assert!(validate_mob_type("dragon").is_ok());
        assert!(validate_mob_type("player").is_ok());
        assert!(validate_mob_type("custom_mob_123").is_ok());
    }

    #[test]
    fn test_validate_mob_type_invalid() {
        assert!(validate_mob_type("").is_err());

        let long_name = "a".repeat(101);
        assert!(validate_mob_type(&long_name).is_err());
    }
}
