use crate::cli::OutputFormat;
use crate::envelope::Envelope;
use anyhow::Result;
use serde_json::json;

pub fn execute(mob: &str, x: i32, y: i32, output: &OutputFormat) -> Result<()> {
    // Validate inputs
    if mob.trim().is_empty() {
        anyhow::bail!("Mob name cannot be empty");
    }

    let envelope = Envelope::success(
        "spawn".to_string(),
        Some(json!({
            "mob": mob,
            "x": x,
            "y": y
        })),
    );

    match output {
        OutputFormat::Json => {
            println!("{}", envelope.to_json()?);
        }
        OutputFormat::Text => {
            println!("Spawning mob '{}' at coordinates ({}, {})", mob, x, y);
        }
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_spawn_valid() {
        let result = execute("goblin", 10, 20, &OutputFormat::Text);
        assert!(result.is_ok());
    }

    #[test]
    fn test_spawn_empty_mob() {
        let result = execute("", 10, 20, &OutputFormat::Text);
        assert!(result.is_err());
    }

    #[test]
    fn test_spawn_whitespace_mob() {
        let result = execute("   ", 10, 20, &OutputFormat::Text);
        assert!(result.is_err());
    }
}
