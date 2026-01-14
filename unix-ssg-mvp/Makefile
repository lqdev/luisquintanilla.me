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

# Required tools check and installation
REQUIRED_TOOLS := pandoc yq envsubst find grep sed awk sort uniq

# Check if tools are available, install if missing
install-deps:
	@echo "🔍 Checking for required tools..."
	@MISSING_TOOLS=""; \
	for tool in pandoc yq; do \
		if ! command -v $$tool >/dev/null 2>&1; then \
			MISSING_TOOLS="$$MISSING_TOOLS $$tool"; \
		fi; \
	done; \
	if [ -n "$$MISSING_TOOLS" ]; then \
		echo "📦 Installing missing tools:$$MISSING_TOOLS"; \
		if command -v apt-get >/dev/null 2>&1; then \
			sudo apt-get update -qq && sudo apt-get install -y$$MISSING_TOOLS; \
		elif command -v brew >/dev/null 2>&1; then \
			brew install$$MISSING_TOOLS; \
		else \
			echo "❌ Could not find package manager (apt-get or brew)"; \
			echo "Please install manually: apt-get install pandoc yq"; \
			exit 1; \
		fi; \
	fi
	@echo "✅ All required tools available"

check-deps: install-deps
	@for tool in $(REQUIRED_TOOLS); do \
		if ! command -v $$tool >/dev/null 2>&1; then \
			echo "❌ Missing required tool: $$tool"; \
			exit 1; \
		fi; \
	done
	@echo "✅ Dependencies verified"

# Clean build directory
clean:
	@echo "🧹 Cleaning build directory..."
	rm -rf $(BUILD_DIR)/*
	@echo "✅ Clean complete"

# Create directory structure
dirs:
	@echo "📁 Creating build directories..."
	mkdir -p $(BUILD_DIR)/{posts,feed,assets,tags,playlists,reviews,notes,.metadata}
	@echo "✅ Directories created"

# Make scripts executable
prepare-scripts:
	@chmod +x $(BIN_DIR)/*.sh 2>/dev/null || true

# Process markdown content
process-content: dirs prepare-scripts
	@echo "📝 Processing markdown content..."
	@chmod +x $(BIN_DIR)/*.sh 2>/dev/null || true
	@find $(SRC_DIR) -name "*.md" -type f 2>/dev/null | while read -r file; do \
		$(BIN_DIR)/process-markdown.sh "$$file" $(BUILD_DIR) $(SRC_DIR) 2>&1 | grep -v "^$$" || true; \
	done
	@echo "✅ Content processing complete"

# Generate feeds
generate-feeds: prepare-scripts
	@echo "📡 Generating RSS feeds..."
	@$(BIN_DIR)/generate-feeds.sh $(BUILD_DIR) $(SRC_DIR) 2>/dev/null || true
	@echo "✅ Feed generation complete"

# Copy assets
copy-assets:
	@echo "📦 Copying static assets..."
	@if [ -d "$(SRC_DIR)/assets" ]; then \
		cp -r $(SRC_DIR)/assets/* $(BUILD_DIR)/assets/ 2>/dev/null || true; \
	fi
	@echo "✅ Assets copied"

# Generate tag pages
generate-tags: prepare-scripts
	@echo "🏷️ Generating tag pages..."
	@$(BIN_DIR)/simple-tags.sh 2>/dev/null || true
	@echo "✅ Tag pages generated"

# Generate root index
generate-index: prepare-scripts
	@echo "🏠 Generating root index..."
	@$(BIN_DIR)/generate-index.sh $(BUILD_DIR) $(SRC_DIR) 2>/dev/null || true
	@echo "✅ Root index generated"

# Generate collection indexes
generate-collection-indexes: prepare-scripts
	@echo "📚 Generating collection indexes..."
	@$(BIN_DIR)/generate-collection-indexes.sh $(BUILD_DIR) $(SRC_DIR) 2>/dev/null || true
	@echo "✅ Collection indexes generated"

# Main build target
build: check-deps clean prepare-scripts process-content generate-feeds copy-assets generate-tags generate-index generate-collection-indexes
	@echo "🎉 Build complete! Output in $(BUILD_DIR)/"
	@echo ""
	@echo "📁 Generated files:"
	@find $(BUILD_DIR) -name "index.html" | wc -l | xargs echo "   HTML pages:"
	@find $(BUILD_DIR) -name "*.xml" | wc -l | xargs echo "   RSS feeds:"
	@echo ""
	@echo "🚀 To serve locally:"
	@echo "   make serve"

# Development server (if python3 available)
serve:
	@echo "🚀 Starting development server at http://localhost:8000"
	@cd $(BUILD_DIR) && python3 -m http.server 8000 2>/dev/null || \
		echo "⚠️ Python3 not available for dev server, serve $(BUILD_DIR) manually"

# Parallel build (experimental)
build-parallel: check-deps clean dirs
	@echo "⚡ Running parallel build..."
	@$(MAKE) -j4 process-content process-enhanced generate-feeds copy-assets generate-tags
	@echo "🎉 Parallel build complete!"

.PHONY: install-deps check-deps prepare-scripts clean dirs process-content generate-feeds copy-assets generate-tags generate-index generate-collection-indexes build serve build-parallel