use clap::{Parser, Subcommand, ValueEnum};

#[derive(Parser)]
#[command(name = "dev-tool")]
#[command(about = "Development tool for Pigeon Pea game management", long_about = None)]
pub struct Cli {
    #[command(subcommand)]
    pub command: Commands,

    /// Output format (text or json)
    #[arg(long, value_enum, default_value = "text", global = true)]
    pub output: OutputFormat,
}

#[derive(Subcommand)]
pub enum Commands {
    /// Spawn a mob at specified coordinates
    Spawn {
        /// Name of the mob to spawn
        #[arg(long)]
        mob: String,

        /// X coordinate
        #[arg(long, allow_hyphen_values = true)]
        x: i32,

        /// Y coordinate
        #[arg(long, allow_hyphen_values = true)]
        y: i32,
    },
    /// Teleport to specified coordinates
    Tp {
        /// X coordinate
        #[arg(long, allow_hyphen_values = true)]
        x: i32,

        /// Y coordinate
        #[arg(long, allow_hyphen_values = true)]
        y: i32,
    },
    /// Reload the game configuration
    Reload,
    /// Regenerate the map
    RegenMap {
        /// Optional seed for map generation
        #[arg(long)]
        seed: Option<i32>,
    },
}

#[derive(Clone, ValueEnum)]
pub enum OutputFormat {
    Text,
    Json,
}
