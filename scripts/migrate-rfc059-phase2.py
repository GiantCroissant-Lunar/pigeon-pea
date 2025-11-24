#!/usr/bin/env python3
"""
RFC-059 Phase 2: Rename Plugins to Plugin
Standardizes on Plugin (singular) instead of mixed Plugin/Plugins naming
"""

import os
import re
import subprocess
import sys
from pathlib import Path

def run_git(args):
    """Run git command"""
    result = subprocess.run(['git'] + args, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Git command failed: {result.stderr}", file=sys.stderr)
        sys.exit(1)
    return result.stdout

def update_file_content(file_path, old_name, new_name):
    """Update file content replacing old_name with new_name"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Replace in namespaces and using statements
        updated = re.sub(r'\bnamespace\s+' + re.escape(old_name) + r'\b', f'namespace {new_name}', content)
        updated = re.sub(r'\busing\s+' + re.escape(old_name) + r'\b', f'using {new_name}', updated)
        updated = updated.replace(old_name, new_name)
        
        if updated != content:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(updated)
            return True
        return False
    except Exception as e:
        print(f"Error updating {file_path}: {e}", file=sys.stderr)
        return False

def update_project_files(project_dir, old_name, new_name):
    """Update .csproj and .cs files in a project directory"""
    updated_files = []
    csproj_path = None
    
    # First pass: find and rename .csproj file
    for root, dirs, files in os.walk(project_dir):
        for file in files:
            if file == f"{old_name}.csproj":
                old_csproj = os.path.join(root, file)
                new_csproj = os.path.join(root, f"{new_name}.csproj")
                run_git(['mv', old_csproj, new_csproj])
                csproj_path = new_csproj
                break
    
    # Second pass: update content in all files
    for root, dirs, files in os.walk(project_dir):
        for file in files:
            if file.endswith(('.cs', '.csproj')):
                file_path = os.path.join(root, file)
                if update_file_content(file_path, old_name, new_name):
                    updated_files.append(file_path)
    
    return updated_files

def update_all_references(dotnet_root, old_name, new_name):
    """Update all references in dotnet directory"""
    updated_files = []
    
    for root, dirs, files in os.walk(dotnet_root):
        for file in files:
            if file.endswith(('.cs', '.csproj')):
                file_path = os.path.join(root, file)
                if update_file_content(file_path, old_name, new_name):
                    updated_files.append(file_path)
    
    return updated_files

def rename_plugin(src_dir, plugin_name, is_test=False):
    """Rename a plugin project"""
    old_name = plugin_name
    new_name = old_name.replace('.Plugins.', '.Plugin.')
    
    if old_name == new_name:
        return None
    
    project_path = os.path.join(src_dir, old_name)
    if not os.path.exists(project_path):
        return None
    
    print(f"  Processing {old_name}...")
    
    # Update files in the project
    updated_files = update_project_files(project_path, old_name, new_name)
    if updated_files:
        print(f"    Updated {len(updated_files)} file(s) in project")
    
    # Rename the directory using git mv
    new_project_path = os.path.join(src_dir, new_name)
    run_git(['mv', project_path, new_project_path])
    print(f"    Renamed directory to {new_name}")
    
    # Update all references in dotnet directory
    dotnet_root = os.path.abspath('dotnet')
    ref_files = update_all_references(dotnet_root, old_name, new_name)
    if ref_files:
        print(f"    Updated {len(ref_files)} reference(s) across codebase")
    
    return new_name

def main():
    print("\nPhase 2: Rename Plugins to Plugin")
    print("==================================\n")
    
    # Change to repository root
    os.chdir(Path(__file__).parent.parent)
    
    # Create tests directory for app-essential plugins
    app_tests_dir = os.path.join('dotnet', 'app-essential', 'plugins', 'tests')
    os.makedirs(app_tests_dir, exist_ok=True)
    print("✓ Created app-essential/plugins/tests/ directory\n")
    
    # Process app-essential plugins
    print("Processing app-essential/plugins...")
    app_src_dir = os.path.join('dotnet', 'app-essential', 'plugins', 'src')
    
    app_plugins = [
        d for d in os.listdir(app_src_dir)
        if os.path.isdir(os.path.join(app_src_dir, d)) and 'Plugins' in d
    ]
    
    test_projects = []
    for plugin in sorted(app_plugins):
        is_test = plugin.endswith('.Tests')
        new_name = rename_plugin(app_src_dir, plugin, is_test)
        if new_name and is_test:
            test_projects.append(new_name)
    
    # Move test projects to tests directory
    if test_projects:
        print("\nMoving test projects to tests/...")
        for test_project in test_projects:
            src_path = os.path.join(app_src_dir, test_project)
            dest_path = os.path.join(app_tests_dir, test_project)
            if os.path.exists(src_path):
                run_git(['mv', src_path, dest_path])
                print(f"  ✓ Moved {test_project}")
    
    # Process game-essential plugins
    print("\nProcessing game-essential/plugins...")
    game_src_dir = os.path.join('dotnet', 'game-essential', 'plugins', 'src')
    
    game_plugins = [
        d for d in os.listdir(game_src_dir)
        if os.path.isdir(os.path.join(game_src_dir, d)) and 'Plugins' in d
    ]
    
    for plugin in sorted(game_plugins):
        rename_plugin(game_src_dir, plugin, False)
    
    print("\n✓ Phase 2 Complete!")
    print("Run dotnet build to verify changes.\n")

if __name__ == '__main__':
    main()
