# Unix Static Site Generator MVP
# Minimal static site generator using standard Unix tools

SHELL := /bin/bash
.ONESHELL:
.DEFAULT_GOAL := build

# Configuration
SRC_DIR := src
BUILD_DIR := build
TEMPLATES_DIR := templates
BIN_DIR := bin

# Required tools check
REQUIRED_TOOLS := pandoc yq envsubst find grep sed awk sort uniq
check-deps:
	@for tool in $(REQUIRED_TOOLS); do \
		if ! command -v $$tool >/dev/null 2>&1; then \
			echo "❌ Missing required tool: $$tool"; \
			exit 1; \
		fi; \
	done
	@echo "✅ All required tools available"

# Clean build directory
clean:
	@echo "🧹 Cleaning build directory..."
	rm -rf $(BUILD_DIR)/*
	@echo "✅ Clean complete"

# Create directory structure
dirs:
	@echo "📁 Creating build directories..."
	mkdir -p $(BUILD_DIR)/{posts,feed,assets,tags}
	@echo "✅ Directories created"

# Process markdown content
process-content: dirs
	@echo "📝 Processing markdown content..."
	@find $(SRC_DIR) -name "*.md" -type f | while read -r file; do \
		$(BIN_DIR)/process-markdown.sh "$$file"; \
	done
	@echo "✅ Content processing complete"

# Generate feeds
generate-feeds:
	@echo "📡 Generating RSS feeds..."
	$(BIN_DIR)/generate-feeds.sh
	@echo "✅ Feed generation complete"

# Copy assets
copy-assets:
	@echo "📦 Copying static assets..."
	if [ -d "$(SRC_DIR)/assets" ]; then \
		cp -r $(SRC_DIR)/assets/* $(BUILD_DIR)/assets/ 2>/dev/null || true; \
	fi
	@echo "✅ Assets copied"

# Generate tag pages
generate-tags:
	@echo "🏷️ Generating tag pages..."
	$(BIN_DIR)/generate-tags.sh
	@echo "✅ Tag pages generated"

# Main build target
build: check-deps clean process-content generate-feeds copy-assets generate-tags
	@echo "🎉 Build complete! Output in $(BUILD_DIR)/"

# Development server (if python3 available)
serve:
	@echo "🚀 Starting development server at http://localhost:8000"
	@cd $(BUILD_DIR) && python3 -m http.server 8000 2>/dev/null || \
		echo "⚠️ Python3 not available for dev server, serve $(BUILD_DIR) manually"

# Parallel build (experimental)
build-parallel: check-deps clean dirs
	@echo "⚡ Running parallel build..."
	@$(MAKE) -j4 process-content generate-feeds copy-assets generate-tags
	@echo "🎉 Parallel build complete!"

.PHONY: check-deps clean dirs process-content generate-feeds copy-assets generate-tags build serve build-parallel