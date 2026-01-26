# Trading Platform Overview

## Purpose

The Bipins.AI.Trading platform is an **AI Agentic Trading Bot** demonstration system designed for paper trading with Alpaca. It showcases autonomous decision-making, tool use, reasoning loops, and RAG patterns using LLMs. The platform provides a complete event-driven architecture for automated trading decisions, risk management, and portfolio tracking.

## Key Features

- **🤖 AI Agentic Bot**: Autonomous LLM-powered trading agent that makes independent decisions using:
  - **Tool Use**: Function calling to gather market data, calculate indicators, check portfolio status
  - **Multi-Step Reasoning**: Plans and executes complex analysis before making decisions
  - **RAG Pattern**: Retrieves similar past trading scenarios from vector memory for context
  - **Learning**: Stores decisions and outcomes for continuous improvement
  
- **🔄 Multi-Provider LLM Support**: Configurable support for OpenAI, Anthropic, and Azure OpenAI

- **Event-Driven Architecture**: Uses MassTransit for asynchronous message processing

- **Broker Abstraction**: Supports Alpaca initially, designed to support other brokers

- **Trading Modes**: Ask (manual approval) or Auto (automatic execution)

- **Risk Management**: Built-in guardrails and risk checks

- **Decision Engines**: 
  - **AI Agent** (primary): Uses LLM with function calling for autonomous decisions
  - **Deterministic RSI/MACD** (fallback): Traditional technical analysis strategy

- **Web Portal**: Complete MVC interface for monitoring, control, and agent interaction

- **Vector Storage**: Qdrant integration for agent memory and scenario retrieval

## Architecture

The solution follows Clean Architecture principles:

- **Domain**: Core business entities and value objects
- **Application**: Use cases, ports/interfaces, message contracts
- **Infrastructure**: External adapters (Alpaca, EF Core, Qdrant, Cache)
- **Web**: ASP.NET MVC portal + background hosted services

## Trading Flow (AI Agent)

1. Market data ingestion polls Alpaca for candles
2. Features and indicators are computed from candles
3. **AI Agent builds context**: Gathers market data, portfolio status, indicators
4. **AI Agent searches memory**: Retrieves similar past scenarios from Qdrant (RAG)
5. **AI Agent reasons**: LLM analyzes context + similar scenarios using tools
6. **AI Agent decides**: Makes autonomous trading decision (Buy/Sell/Hold) with confidence and rationale
7. **AI Agent stores**: Decision and context stored as embedding in Qdrant for learning
8. Risk manager validates decisions
9. In Ask mode: user approval required
10. In Auto mode: automatic execution
11. Orders submitted to broker
12. Portfolio updated on fills

## Safety Features

- Trading disabled by default
- Hard risk guardrails always enforced
- Idempotent message handling
- Correlation ID tracking
- Comprehensive logging
