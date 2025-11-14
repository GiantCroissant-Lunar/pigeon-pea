use crate::cli::OutputFormat;
use crate::envelope::Envelope;
use anyhow::Result;
use serde_json::json;

pub fn execute(x: i32, y: i32, output: &OutputFormat) -> Result<()> {
    let envelope = Envelope::success(
        "tp".to_string(),
        Some(json!({
            "x": x,
            "y": y
        })),
    );

    match output {
        OutputFormat::Json => {
            println!("{}", envelope.to_json()?);
        }
        OutputFormat::Text => {
            println!("Teleporting to coordinates ({}, {})", x, y);
        }
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_tp_valid() {
        let result = execute(5, 15, &OutputFormat::Text);
        assert!(result.is_ok());
    }

    #[test]
    fn test_tp_negative_coords() {
        let result = execute(-5, -10, &OutputFormat::Text);
        assert!(result.is_ok());
    }
}
