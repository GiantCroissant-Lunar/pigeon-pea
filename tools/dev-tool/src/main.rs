mod protocol;
mod ws;

use anyhow::Result;
use clap::{Parser, Subcommand, ValueEnum};
use serde::{Deserialize, Serialize};
use std::path::PathBuf;

const DEFAULT_SERVER: &str = "ws://127.0.0.1:5007/gm";

/// Yazi-integrated Rust CLI for pigeon-pea development
#[derive(Parser, Debug)]
#[command(name = "dev-tool")]
#[command(version, about, long_about = None)]
struct Cli {
    /// WebSocket server URL
    #[arg(
        long,
        env = "DEV_TOOL_SERVER",
        default_value = DEFAULT_SERVER
    )]
    server: String,

    /// Authentication token
    #[arg(long, env = "DEV_TOOL_TOKEN")]
    token: Option<String>,

    /// Output format
    #[arg(long, value_enum)]
    output: Option<OutputFormat>,

    /// Request timeout in milliseconds
    #[arg(long)]
    timeout: Option<u64>,

    /// Enable verbose logging
    #[arg(short, long)]
    verbose: bool,

    #[command(subcommand)]
    command: Option<Commands>,
}

#[derive(Debug, Clone, ValueEnum, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
enum OutputFormat {
    Text,
    Json,
}

#[derive(Debug, Subcommand)]
enum Commands {
    /// Connect to the WebSocket server and send a noop command
    Connect {
        /// Optional custom server URL
        #[arg(long)]
        url: Option<String>,
    },

    /// Send a message to the server (stub)
    Send {
        /// Message to send
        message: String,
    },

    /// List available operations (stub)
    List,

    /// Show current configuration
    Config,
}

#[derive(Debug, Serialize, Deserialize)]
struct Config {
    server: Option<String>,
    token: Option<String>,
    output: Option<String>,
    timeout: Option<u64>,
}

impl Config {
    fn load() -> Result<Option<Self>> {
        let config_path = get_config_path()?;
        if config_path.exists() {
            let contents = std::fs::read_to_string(&config_path)?;
            let config: Config = toml::from_str(&contents)?;
            Ok(Some(config))
        } else {
            Ok(None)
        }
    }
}

fn get_config_path() -> Result<PathBuf> {
    let config_dir = dirs::config_dir()
        .ok_or_else(|| anyhow::anyhow!("Could not determine config directory"))?
        .join("dev-tool");
    Ok(config_dir.join("config.toml"))
}

fn merge_config(cli: &mut Cli, file_config: Option<Config>) {
    if let Some(config) = file_config {
        // CLI flags > ENV > config file precedence
        // Only apply config file values if CLI/ENV didn't provide them

        // Server: Check if it's still the default value from clap
        if cli.server == DEFAULT_SERVER && std::env::var("DEV_TOOL_SERVER").is_err() {
            if let Some(server) = config.server {
                cli.server = server;
            }
        }

        // Token: only from config if not in CLI or ENV
        if cli.token.is_none() {
            cli.token = config.token;
        }

        // Output: only from config if not explicitly set
        if cli.output.is_none() {
            if let Some(output) = config.output {
                cli.output = Some(if output == "json" {
                    OutputFormat::Json
                } else {
                    OutputFormat::Text
                });
            }
        }

        // Timeout: only from config if not explicitly set
        if cli.timeout.is_none() {
            cli.timeout = config.timeout;
        }
    }

    // Apply final defaults if still not set
    if cli.output.is_none() {
        cli.output = Some(OutputFormat::Text);
    }
    if cli.timeout.is_none() {
        cli.timeout = Some(5000);
    }
}

fn mask_token(token: &str) -> String {
    if token.len() > 4 {
        format!("{}...", &token[..4])
    } else {
        "***".to_string()
    }
}

fn print_config(cli: &Cli) {
    if cli.verbose {
        eprintln!("=== Effective Configuration ===");
        eprintln!("Server:  {}", cli.server);
        eprintln!(
            "Token:   {}",
            cli.token
                .as_ref()
                .map(|t| mask_token(t))
                .unwrap_or_else(|| "None".to_string())
        );
        eprintln!("Output:  {:?}", cli.output.as_ref().unwrap());
        eprintln!("Timeout: {}ms", cli.timeout.unwrap());
        eprintln!("Config:  {}", get_config_path().unwrap().display());
        eprintln!("==============================\n");
    }
}

#[tokio::main]
async fn main() -> Result<()> {
    let mut cli = Cli::parse();

    // Load configuration file
    let file_config = match Config::load() {
        Ok(config) => config,
        Err(e) => {
            if cli.verbose {
                eprintln!("Warning: Failed to load config file: {}", e);
            }
            None
        }
    };

    // Merge configurations (CLI > ENV > config file)
    merge_config(&mut cli, file_config);

    // Print effective configuration if verbose
    print_config(&cli);

    match cli.command {
        Some(Commands::Connect { url }) => {
            let server_url = url.unwrap_or_else(|| cli.server.clone());
            let client = ws::WsClient::new(server_url, cli.token.clone());
            client.connect().await
        }
        Some(Commands::Send { message }) => {
            println!("Sending message: {}", message);
            println!("(Send functionality not yet implemented)");
            Ok(())
        }
        Some(Commands::List) => {
            println!("Available operations:");
            println!("  - connect: Connect to WebSocket server");
            println!("  - send:    Send a message");
            println!("  - list:    List operations");
            println!("  - config:  Show configuration");
            println!("(Full operation listing not yet implemented)");
            Ok(())
        }
        Some(Commands::Config) => {
            let output_fmt = cli.output.as_ref().unwrap();
            let timeout_val = cli.timeout.unwrap();
            if matches!(output_fmt, OutputFormat::Json) {
                let output_str = match output_fmt {
                    OutputFormat::Text => "text",
                    OutputFormat::Json => "json",
                };
                let masked_token = cli.token.as_ref().map(|t| mask_token(t));
                let config = serde_json::json!({
                    "server": cli.server,
                    "token": masked_token,
                    "output": output_str,
                    "timeout": timeout_val,
                    "config_file": get_config_path()?.display().to_string(),
                });
                println!("{}", serde_json::to_string_pretty(&config)?);
            } else {
                println!("=== Current Configuration ===");
                println!("Server:      {}", cli.server);
                println!(
                    "Token:       {}",
                    cli.token
                        .as_ref()
                        .map(|t| mask_token(t))
                        .unwrap_or_else(|| "None".to_string())
                );
                println!("Output:      {:?}", output_fmt);
                println!("Timeout:     {}ms", timeout_val);
                println!("Config File: {}", get_config_path()?.display());
                println!("============================");
            }
            Ok(())
        }
        None => {
            println!("dev-tool: Yazi-integrated Rust CLI");
            println!("\nUse --help to see available options and commands");
            println!("\nExample usage:");
            println!("  dev-tool --server ws://localhost:8080 connect");
            println!("  dev-tool --output json config");
            println!("  dev-tool --help");
            Ok(())
        }
    }
}
