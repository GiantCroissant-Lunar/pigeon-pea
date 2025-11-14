mod cli;
mod commands;
mod envelope;

use clap::Parser;
use cli::{Cli, Commands};
use std::process;

fn main() {
    let cli = Cli::parse();

    let result = match &cli.command {
        Commands::Spawn { mob, x, y } => commands::spawn::execute(mob, *x, *y, &cli.output),
        Commands::Tp { x, y } => commands::tp::execute(*x, *y, &cli.output),
        Commands::Reload => commands::reload::execute(&cli.output),
        Commands::RegenMap { seed } => commands::regen_map::execute(*seed, &cli.output),
    };

    match result {
        Ok(()) => process::exit(0),
        Err(e) => {
            eprintln!("Error: {}", e);
            process::exit(1);
        }
    }
}
