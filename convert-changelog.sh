#!/bin/bash

# Convert CHANGELOG.md to Debian changelog format
set -e

CHANGELOG_MD="CHANGELOG.md"
DEBIAN_CHANGELOG="debian/changelog"
MAINTAINER="Sean McElroy <me@seanmcelroy.com>"

if [ ! -f "$CHANGELOG_MD" ]; then
    echo "ERROR: $CHANGELOG_MD not found"
    exit 1
fi

echo "Converting $CHANGELOG_MD to $DEBIAN_CHANGELOG..."

# Ensure debian directory exists
mkdir -p debian

# Function to convert ISO date to Debian format
convert_to_debian_date() {
    local iso_date=$1
    # Convert YYYY-MM-DD to RFC 2822 format
    date -d "$iso_date" -R
}

# Create temporary file
TEMP_FILE=$(mktemp)

# Process CHANGELOG.md
python3 -c "
import re
import sys
import textwrap

def wrap_changelog_line(text, width=76):
    '''Wrap text to fit Debian changelog format (80 chars with 4-char indent)'''
    if len(text) <= width:
        return f'  * {text}'
    
    # Use textwrap to break long lines
    wrapped = textwrap.fill(text, width=width, 
                           initial_indent='  * ',
                           subsequent_indent='    ')
    return wrapped

with open('$CHANGELOG_MD', 'r') as f:
    content = f.read()

# Split by version headers with dates: ## [version] - date
version_matches = re.findall(r'^## \[([^\]]+)\] - (\d{4}-\d{2}-\d{2})', content, flags=re.MULTILINE)

# Split content by version headers to get the content for each version
version_sections = re.split(r'^## \[[^\]]+\] - \d{4}-\d{2}-\d{2}', content, flags=re.MULTILINE)[1:]

for i, ((version, date), section_content) in enumerate(zip(version_matches, version_sections)):
    print(f'jot ({version}) unstable; urgency=low')
    print()
    
    # Extract bullet points (lines starting with '- ')
    lines = section_content.split('\n')
    for line in lines:
        line = line.strip()
        if line.startswith('- '):
            # Remove leading '- ' and format as Debian changelog entry
            change = line[2:].strip()
            if change:
                # Remove backticks and wrap lines properly
                change = change.replace('\`', '')
                print(wrap_changelog_line(change))
    
    print()
    print(f' -- $MAINTAINER  \$(convert_debian_date {date})')
    print()
" > "$TEMP_FILE"

# Replace the date placeholders with actual dates
sed 's/\$(convert_debian_date \([^)]*\))/PLACEHOLDER_\1/g' "$TEMP_FILE" | while IFS= read -r line; do
    if [[ $line == *"PLACEHOLDER_"* ]]; then
        # Extract ISO date from PLACEHOLDER_YYYY-MM-DD
        if [[ $line =~ PLACEHOLDER_([0-9]{4}-[0-9]{2}-[0-9]{2}) ]]; then
            iso_date="${BASH_REMATCH[1]}"
            debian_date=$(convert_to_debian_date "$iso_date")
            line="${line//PLACEHOLDER_$iso_date/$debian_date}"
        fi
    fi
    echo "$line"
done > "$DEBIAN_CHANGELOG"

# Clean up
rm "$TEMP_FILE"

echo "Successfully converted $CHANGELOG_MD to $DEBIAN_CHANGELOG"