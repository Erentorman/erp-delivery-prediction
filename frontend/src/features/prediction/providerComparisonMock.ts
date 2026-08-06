import type { RuleBasedPredictionResult } from './predictionContracts';

export interface MockAiPrediction {
  estimatedDelivery: string;
  modelVersion: string;
  confidenceScore: number;
  warnings: string[];
}

export interface MockHybridPrediction {
  estimatedDelivery: string;
  ruleWeight: number;
  aiWeight: number;
}

export interface ProviderComparison {
  ai: MockAiPrediction;
  hybrid: MockHybridPrediction;
}

const ONE_DAY_MS = 24 * 60 * 60 * 1000;

/**
 * AI ve Final Hybrid tahmin sağlayıcıları backend'de henüz uygulanmadı.
 * Bu fonksiyon üç kartlı karşılaştırma düzenini önizlemek için ÖRNEK veri üretir;
 * hiçbir gerçek model çağrısı yapmaz. Rule-Based sonucu (parametre) gerçek API verisidir,
 * yalnızca ai/hybrid alanları buradan üretilir ve UI'da açıkça "örnek veri" olarak işaretlenmelidir.
 */
export function buildMockProviderComparison(ruleBased: RuleBasedPredictionResult): ProviderComparison {
  const ruleDeliveryMs = new Date(ruleBased.estimatedDelivery).getTime();
  const mockAiDeliveryMs = Number.isFinite(ruleDeliveryMs) ? ruleDeliveryMs + ONE_DAY_MS : ruleDeliveryMs;

  return {
    ai: {
      estimatedDelivery: new Date(mockAiDeliveryMs).toISOString(),
      modelVersion: 'örnek-v0',
      confidenceScore: 0.85,
      warnings: ruleBased.appliedFallbackReasons.length > 0
        ? ['Kısıtlara bağlı olası gecikme (örnek uyarı)']
        : [],
    },
    hybrid: {
      estimatedDelivery: ruleBased.estimatedDelivery,
      ruleWeight: 0.7,
      aiWeight: 0.3,
    },
  };
}
