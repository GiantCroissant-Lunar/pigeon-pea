use crate::cli::OutputFormat;
use crate::envelope::Envelope;
use anyhow::Result;
use serde_json::json;

pub fn execute(seed: Option<i32>, output: &OutputFormat) -> Result<()> {
    let args = Some(match seed {
        Some(s) => json!({ "seed": s }),
        None => json!({}),
    });

    let envelope = Envelope::success("regen-map".to_string(), args);

    match output {
        OutputFormat::Json => {
            println!("{}", envelope.to_json()?);
        }
        OutputFormat::Text => {
            if let Some(s) = seed {
                println!("Regenerating map with seed: {}", s);
            } else {
                println!("Regenerating map with random seed");
            }
        }
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_regen_map_with_seed() {
        let result = execute(Some(12345), &OutputFormat::Text);
        assert!(result.is_ok());
    }

    #[test]
    fn test_regen_map_without_seed() {
        let result = execute(None, &OutputFormat::Text);
        assert!(result.is_ok());
    }
}
