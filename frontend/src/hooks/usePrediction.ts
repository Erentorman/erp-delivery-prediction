import { useState, useCallback } from 'react';

// Common Types
export interface TimelineItem {
  operationRef: string;
  estimatedStart: string;
  estimatedEnd: string;
  isCritical: boolean;
}

export interface MaterialShortage {
  materialRef: string;
  missingQuantity: number;
}

export interface RuleBasedPrediction {
  orderReference: string;
  estimatedStart: string;
  estimatedEnd: string;
  estimatedDelivery: string;
  criticalPathOperations: string[];
  appliedFallbackReasons: string[];
  shortages: MaterialShortage[];
  timeline: TimelineItem[];
  displayWorkingLeadTime?: number; // Calculated field
}

export interface AiPrediction {
  estimatedDelivery: string;
  modelVersion: string;
  confidenceScore: number;
  warnings: string[];
}

export interface HybridPrediction {
  estimatedDelivery: string;
  ruleWeight: number;
  aiWeight: number;
  displayWorkingLeadTime: number;
}

export interface PredictionFactors {
  riskLevel: 'Low' | 'Medium' | 'High';
  factors: { name: string; impact: string }[];
}

// Planner View Result (Tripartite)
export interface PlannerCalculationResult {
  ruleBased: RuleBasedPrediction;
  ai: AiPrediction;
  hybrid: HybridPrediction;
  criticalPathSummary: TimelineItem[];
  factors: PredictionFactors;
}

// Customer View Result
export interface CustomerSimulationResult {
  orderReference: string;
  finalDeliveryDate: string;
  displayWorkingLeadTime: number;
}

export function usePrediction() {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [customerResult, setCustomerResult] = useState<CustomerSimulationResult | null>(null);
  const [plannerResult, setPlannerResult] = useState<PlannerCalculationResult | null>(null);

  // MOCK AI and Hybrid data since Backend T-386 only provides RuleBased currently
  const generateMockAiAndHybrid = (ruleBased: RuleBasedPrediction): PlannerCalculationResult => {
    // Add 1 day for AI prediction to simulate a difference
    const ruleDate = new Date(ruleBased.estimatedDelivery);
    const aiDate = new Date(ruleDate.getTime() + 24 * 60 * 60 * 1000);
    
    // Convert duration to hours for display
    const start = new Date(ruleBased.estimatedStart).getTime();
    const end = new Date(ruleBased.estimatedEnd).getTime();
    const hours = Math.max(1, Math.round((end - start) / (1000 * 60 * 60)));

    return {
      ruleBased: { ...ruleBased, displayWorkingLeadTime: hours },
      ai: {
        estimatedDelivery: aiDate.toISOString(),
        modelVersion: 'v1.0.0-beta',
        confidenceScore: 0.85,
        warnings: ['Slight delay detected in Station B'],
      },
      hybrid: {
        estimatedDelivery: ruleDate.toISOString(),
        ruleWeight: 0.7,
        aiWeight: 0.3,
        displayWorkingLeadTime: hours,
      },
      criticalPathSummary: ruleBased.timeline.filter(t => t.isCritical),
      factors: {
        riskLevel: ruleBased.appliedFallbackReasons.length > 0 ? 'Medium' : 'Low',
        factors: ruleBased.appliedFallbackReasons.map(r => ({ name: 'Fallback', impact: r }))
      }
    };
  };

  const fetchSimulation = useCallback(async (productRef: string, location: string, quantity: number) => {
    // We log the quantity since we don't have a real simulate endpoint using it yet
    console.log(`Simulating for ${quantity} items of ${productRef} to ${location}`);
    setIsLoading(true);
    setError(null);
    setCustomerResult(null);

    try {
      // In a real scenario, this would hit /api/predictions/simulate
      // Since it doesn't exist yet, we use /calculate with a mock order ID mapped from the product
      // Masa -> SO00004, Sandalye -> SO00001, vb.
      const mockOrderId = productRef === 'P001' ? 'SO00004' 
                        : productRef === 'P002' ? 'SO00001' 
                        : productRef === 'P003' ? 'SO00016'
                        : productRef === 'P004' ? 'SO00003' : 'SO00001';

      const response = await fetch('/api/predictions/calculate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderReference: mockOrderId }),
      });

      if (!response.ok) {
        throw new Error(`Simulation failed: ${response.status}`);
      }

      const ruleBased: RuleBasedPrediction = await response.json();
      
      const start = new Date(ruleBased.estimatedStart).getTime();
      const end = new Date(ruleBased.estimatedEnd).getTime();
      const hours = Math.max(1, Math.round((end - start) / (1000 * 60 * 60)));

      // Extra delay based on location (simulated logic)
      let locationDelayHours = 0;
      if (location === 'Ankara') locationDelayHours = 24;
      if (location === 'İzmir') locationDelayHours = 12;
      if (location === 'Antalya') locationDelayHours = 48;

      const finalDelivery = new Date(ruleBased.estimatedDelivery);
      finalDelivery.setHours(finalDelivery.getHours() + locationDelayHours);

      setCustomerResult({
        orderReference: ruleBased.orderReference,
        finalDeliveryDate: finalDelivery.toISOString(),
        displayWorkingLeadTime: hours + locationDelayHours
      });
    } catch (err: any) {
      setError(err.message || 'Beklenmeyen hata');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const fetchCalculation = useCallback(async (orderReference: string) => {
    setIsLoading(true);
    setError(null);
    setPlannerResult(null);

    try {
      const response = await fetch('/api/predictions/calculate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderReference }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.detail || `Calculation failed: ${response.status}`);
      }

      const ruleBased: RuleBasedPrediction = await response.json();
      const plannerData = generateMockAiAndHybrid(ruleBased);
      setPlannerResult(plannerData);
    } catch (err: any) {
      setError(err.message || 'Beklenmeyen hata');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const resetState = useCallback(() => {
    setCustomerResult(null);
    setPlannerResult(null);
    setError(null);
  }, []);

  return {
    isLoading,
    error,
    customerResult,
    plannerResult,
    fetchSimulation,
    fetchCalculation,
    resetState
  };
}
