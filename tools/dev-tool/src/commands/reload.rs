use crate::cli::OutputFormat;
use crate::envelope::Envelope;
use anyhow::Result;

pub fn execute(output: &OutputFormat) -> Result<()> {
    let envelope = Envelope::success("reload".to_string(), None);

    match output {
        OutputFormat::Json => {
            println!("{}", envelope.to_json()?);
        }
        OutputFormat::Text => {
            println!("Reloading game configuration");
        }
    }

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_reload() {
        let result = execute(&OutputFormat::Text);
        assert!(result.is_ok());
    }
}
